using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using StudyMateAI.Helpers;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.ViewModels
{
    public class ExamViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;

        // -- Setup Properties --
        public ObservableCollection<Document> Documents { get; set; }
        public ObservableCollection<VideoResource> Videos { get; set; }

        private object _selectedSource;
        public object SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource == value) return;
                _selectedSource = value;
                OnPropertyChanged();
                // Reset exam when source changes
                CurrentView = "Setup";
            }
        }

        private bool _isVideosSelected;
        public bool IsVideosSelected
        {
            get => _isVideosSelected;
            set
            {
                if (_isVideosSelected == value) return;
                _isVideosSelected = value;
                OnPropertyChanged();
                SelectedSource = null;
            }
        }

        private int _questionCount = 5;
        public int QuestionCount
        {
            get => _questionCount;
            set { if (_questionCount == value) return; _questionCount = value; OnPropertyChanged(); }
        }

        private string _examType = "Test";
        public string ExamType
        {
            get => _examType;
            set { if (_examType == value) return; _examType = value; OnPropertyChanged(); }
        }

        private string _difficulty = "Orta";
        public string Difficulty
        {
            get => _difficulty;
            set { if (_difficulty == value) return; _difficulty = value; OnPropertyChanged(); }
        }

        // -- Exam State --
        private string _currentView = "Setup"; // Setup, Quiz, Result
        public string CurrentView
        {
            get => _currentView;
            set 
            { 
                if (_currentView == value) return;
                _currentView = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSetupView));
                OnPropertyChanged(nameof(IsQuizView));
                OnPropertyChanged(nameof(IsResultView));
            }
        }

        public bool IsSetupView => CurrentView == "Setup";
        public bool IsQuizView => CurrentView == "Quiz";
        public bool IsResultView => CurrentView == "Result";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(); }
        }

        private string _loadingMessage = "";
        public string LoadingMessage
        {
            get => _loadingMessage;
            set { if (_loadingMessage == value) return; _loadingMessage = value; OnPropertyChanged(); }
        }

        // -- Quiz Properties --
        public ObservableCollection<QuizQuestion> Questions { get; set; } = new ObservableCollection<QuizQuestion>();
        
        private int _currentQuestionIndex;
        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            set 
            { 
                if (_currentQuestionIndex == value) return;
                _currentQuestionIndex = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentQuestion));
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public QuizQuestion CurrentQuestion => Questions.Count > CurrentQuestionIndex ? Questions[CurrentQuestionIndex] : null;

        public string ProgressText => $"Soru {CurrentQuestionIndex + 1} / {Questions.Count}";

        // -- Result Properties --
        private ExamReport _report;
        public ExamReport Report
        {
            get => _report;
            set { if (_report == value) return; _report = value; OnPropertyChanged(); }
        }

        // -- Commands --
        public ICommand StartExamCommand { get; }
        public ICommand SubmitAnswerCommand { get; }
        public ICommand NextQuestionCommand { get; }
        public ICommand ResetExamCommand { get; }
        public ICommand EvalulateOpenEndedCommand { get; }

        public ExamViewModel()
        {
            try
            {
                _databaseService = new DatabaseService();
                _ollamaService = new OllamaService();
                
                Documents = new ObservableCollection<Document>(_databaseService.GetAllDocuments());
                Videos = new ObservableCollection<VideoResource>(_databaseService.GetSavedVideos());

                StartExamCommand = new RelayCommandAsync(StartExam, CanStartExam);
                SubmitAnswerCommand = new RelayCommand(SubmitAnswer);
                NextQuestionCommand = new RelayCommand(NextQuestion);
                ResetExamCommand = new RelayCommand(_ => { CurrentView = "Setup"; Questions.Clear(); Report = null; });
                EvalulateOpenEndedCommand = new RelayCommandAsync(EvaluateOpenEnded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sınav modülü başlatılırken hata oluştu: {ex.Message}", "Hata");
            }
        }

        private bool CanStartExam()
        {
            return SelectedSource != null && !IsLoading;
        }

        private async Task StartExam()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Sınav hazırlanıyor (Yapay Zeka soruları üretiyor)...";

                string content = "";
                if (SelectedSource is Document doc)
                    content = doc.Content;
                else if (SelectedSource is VideoResource vid)
                    content = vid.Transcript;

                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("Seçilen kaynağın içeriği boş (veya transkript yok). Lütfen başka bir kaynak seçin.", "Hata");
                    IsLoading = false;
                    return;
                }

                string jsonResponse = await _ollamaService.GenerateQuizFromContentAsync(content, QuestionCount, Difficulty, ExamType);
                var questions = ParseJson<List<QuizQuestion>>(jsonResponse);

                if (questions == null || questions.Count == 0)
                {
                    // Fallback: Create a single dummy question if AI fails completely, so app doesn't crash or get stuck
                    questions = new List<QuizQuestion>
                    {
                        new QuizQuestion 
                        { 
                            Question = "Yapay zeka soru üretemedi. Lütfen internet bağlantınızı ve Ollama servisini kontrol edip tekrar deneyin.", 
                            Options = new List<string>{ "Hata", "Tekrar Dene", "Ayarları Kontrol Et", "İptal" },
                            CorrectAnswer = 0,
                            Score = 0
                        }
                    };
                    MessageBox.Show("Yapay zeka soru üretirken bir sorunla karşılaştı. Lütfen ayarları kontrol edin.", "Uyarı");
                }

                Questions.Clear();
                foreach (var q in questions) Questions.Add(q);
                
                CurrentQuestionIndex = 0;
                CurrentView = "Quiz";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata");
                IsLoading = false; // Ensure fallback
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SubmitAnswer(object parameter)
        {
            // For Classical, scoring is done via Evaluate button.
            // For Test, we store selection.
            if (ExamType == "Test" && parameter is string option)
            {
               // This logic is handled by binding, but we can double check or auto-advance
            }
        }
        
        private async Task EvaluateOpenEnded()
        {
            if (CurrentQuestion == null) return;
            
            try
            {
                IsLoading = true;
                LoadingMessage = "Cevabınız değerlendiriliyor...";
                
                string jsonResponse = await _ollamaService.EvaluateAnswerAsync(
                    CurrentQuestion.Question, 
                    CurrentQuestion.UserAnswerText ?? "", 
                    CurrentQuestion.ModelAnswer ?? "");
                
                // Expecting { "Score": 80, "Feedback": "..." }
                dynamic result = ParseJson<dynamic>(jsonResponse); // Use safe parser
                
                if (result != null)
                {
                    CurrentQuestion.Score = (double)result.Score;
                    CurrentQuestion.Feedback = (string)result.Feedback;
                    CurrentQuestion.IsEvaluated = true;
                    // Force UI update
                    OnPropertyChanged(nameof(CurrentQuestion)); 
                }
                else
                {
                    throw new Exception("JSON parse error");
                }
            }
            catch
            {
                // Fallback
                CurrentQuestion.Score = 0;
                CurrentQuestion.Feedback = "Değerlendirme yapılamadı (AI yanıtı okunamadı).";
                CurrentQuestion.IsEvaluated = true;
                OnPropertyChanged(nameof(CurrentQuestion)); 
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NextQuestion(object parameter)
        {
            if (CurrentQuestionIndex < Questions.Count - 1)
            {
                CurrentQuestionIndex++;
            }
            else
            {
                FinishExam();
            }
        }

        private async void FinishExam()
        {
            IsLoading = true;
            LoadingMessage = "Sınav sonucu ve rapor hazırlanıyor...";
            
            try
            {
                // Calculate implicit score for Test
                if (ExamType == "Test")
                {
                    foreach(var q in Questions)
                    {
                        if (q.SelectedOptionIndex == q.CorrectAnswer)
                            q.Score = 100;
                        else
                            q.Score = 0;
                    }
                }

                double totalScore = Questions.Any() ? Questions.Average(q => q.Score) : 0;
                
                // Generate Report
                // Summary string
                string summary = $"Sınav Tipi: {ExamType}\nOrtalama Puan: {totalScore}\n\nDetaylar:\n";
                foreach(var q in Questions)
                {
                    summary += $"- Soru: {q.Question}\n  Puan: {q.Score}\n  Hata/Durum: {(q.Score < 50 ? "Başarısız" : "Başarılı")}\n\n";
                }

                string reportJson = await _ollamaService.GenerateExamReportAsync(summary);
                Report = ParseJson<ExamReport>(reportJson);
                
                if (Report == null)
                {
                    // Robust Fallback Report
                     Report = new ExamReport 
                     { 
                         Score = totalScore, 
                         OverallAssessment = "Yapay zeka detaylı rapor oluşturamadı ancak puanınız hesaplandı.", 
                         WeakTopics = new List<string> { "Belirlenemedi" },
                         Recommendations = new List<string> { "Eksik olduğunuz konuları tekrar ediniz." } 
                     };
                }
                else
                {
                    // Ensure core score matches or use AI score? Let's use calculated score for accuracy
                    Report.Score = totalScore;
                }

                CurrentView = "Result";
            }
            catch (Exception ex)
            {
                  MessageBox.Show($"Rapor oluşturulurken hata: {ex.Message}");
                  // Show result anyway with minimal info
                  if (Report == null) Report = new ExamReport { Score = 0, OverallAssessment = "Hata oluştu." };
                  CurrentView = "Result"; 
            }
            finally
            {
                IsLoading = false;
            }
        }

        private T? ParseJson<T>(string json) where T : class
        {
            try 
            {
                // Extract JSON from markdown code blocks if present
                var match = Regex.Match(json, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
                string cleanJson = match.Success ? match.Groups[1].Value : json;
                
                return JsonConvert.DeserializeObject<T>(cleanJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
