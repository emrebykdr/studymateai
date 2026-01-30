using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using StudyMateAI.Helpers;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.ViewModels
{
    public class VideoPlayerViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;
        private readonly TranscriptService _transcriptService;
        
        private string _videoUrl = "";
        private string _embedUrl = "";
        private string _currentQuestion = "";
        private VideoResource? _currentVideo;
        private string? _currentTranscript;

        public string VideoUrl
        {
            get => _videoUrl;
            set
            {
                _videoUrl = value;
                OnPropertyChanged();
            }
        }

        public string EmbedUrl
        {
            get => _embedUrl;
            set
            {
                _embedUrl = value;
                OnPropertyChanged();
            }
        }

        public string CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                _currentQuestion = value;
                OnPropertyChanged();
            }
        }

        public VideoResource? CurrentVideo
        {
            get => _currentVideo;
            set
            {
                _currentVideo = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<VideoChatMessage> ChatMessages { get; set; }
        public ObservableCollection<VideoResource> SavedVideos { get; set; }

        public ICommand LoadVideoCommand { get; }
        public ICommand SaveVideoCommand { get; }
        public ICommand AskQuestionCommand { get; }
        public ICommand LoadSavedVideoCommand { get; }
        public ICommand EditVideoTitleCommand { get; }
        public ICommand SummarizeVideoCommand { get; }
        public ICommand AnalyzeScreenCommand { get; }

        public event Action<string>? VideoLoaded;
        public event Func<Task<string>>? CaptureScreenRequested;

        public VideoPlayerViewModel()
        {
            _databaseService = new DatabaseService();
            _ollamaService = new OllamaService();
            _transcriptService = new TranscriptService();
            
            ChatMessages = new ObservableCollection<VideoChatMessage>();
            SavedVideos = new ObservableCollection<VideoResource>();
            
            LoadVideoCommand = new RelayCommandAsync(LoadVideo);
            SaveVideoCommand = new RelayCommand((o) => SaveVideo(), (o) => CurrentVideo != null);
            AskQuestionCommand = new RelayCommandAsync(AskQuestion, () => !string.IsNullOrWhiteSpace(CurrentQuestion));
            LoadSavedVideoCommand = new RelayCommand((param) => LoadSavedVideo((VideoResource)param));
            EditVideoTitleCommand = new RelayCommand((param) => EditVideoTitle((VideoResource)param));
            SummarizeVideoCommand = new RelayCommandAsync(SummarizeVideo, () => !string.IsNullOrEmpty(_currentTranscript));
            AnalyzeScreenCommand = new RelayCommandAsync(AnalyzeScreen, () => CurrentVideo != null);
            
            // Load saved videos on startup
            LoadSavedVideos();
        }

        private async Task AnalyzeScreen()
        {
            try
            {
                if (CaptureScreenRequested == null)
                {
                    MessageBox.Show("Ekran görüntüsü alma işlevi kullanılamıyor.", "Hata");
                    return;
                }

                ChatMessages.Add(new VideoChatMessage { Role = "Siz", Content = "📷 Bu ekran görüntüsünü analiz et." });
                ChatMessages.Add(new VideoChatMessage { Role = "AI", Content = "Ekran analiz ediliyor..." });

                string base64Image = await CaptureScreenRequested.Invoke();
                if (string.IsNullOrEmpty(base64Image))
                {
                    ChatMessages.Add(new VideoChatMessage { Role = "Sistem", Content = "Ekran görüntüsü alınamadı." });
                    return;
                }

                string prompt = "Bu ekran görüntüsünde neler görüyorsun? Görüntüdeki içeriği, metinleri ve görsel unsurları detaylıca açıkla.";
                string response = await _ollamaService.SendMessageAsync(prompt, null, base64Image, category: OllamaService.ModelCategory.Video);

                ChatMessages.Add(new VideoChatMessage { Role = "AI", Content = response });
            }
            catch (Exception ex)
            {
                ChatMessages.Add(new VideoChatMessage { Role = "Sistem", Content = $"Analiz hatası: {ex.Message}" });
            }
        }

        private async Task LoadVideo()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(VideoUrl))
                {
                    MessageBox.Show("Lütfen bir YouTube URL'si girin!", "Uyarı");
                    return;
                }

                // Extract video ID from URL
                string videoId = ExtractVideoId(VideoUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    MessageBox.Show("Geçersiz YouTube URL'si!", "Hata");
                    return;
                }

                // Create embed URL
                EmbedUrl = $"https://www.youtube.com/embed/{videoId}";

                // Create video resource object
                CurrentVideo = new VideoResource
                {
                    VideoId = videoId,
                    YouTubeUrl = VideoUrl,
                    Title = "YouTube Video",
                    Duration = 0,
                    Notes = "",
                    DateAdded = DateTime.Now
                };

                // Notify that video is loaded
                VideoLoaded?.Invoke(videoId);

                // Initialize chat
                ChatMessages.Clear();
                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "AI",
                    Content = "Video yükleniyor... Altyazılar kontrol ediliyor."
                });

                // Fetch transcript
                _currentTranscript = await _transcriptService.GetTranscriptAsync(videoId);

                if (!string.IsNullOrEmpty(_currentTranscript))
                {
                     if (CurrentVideo != null) CurrentVideo.Transcript = _currentTranscript;

                     ChatMessages.Add(new VideoChatMessage
                     {
                         Role = "AI",
                         Content = "✅ Video altyazıları başarıyla yüklendi! Videoyu özetleyebilir veya içeriği hakkında detaylı sorular sorabilirsiniz."
                     });
                }
                else
                {
                    ChatMessages.Add(new VideoChatMessage
                    {
                        Role = "AI",
                        Content = "⚠️ Bu video için altyazı bulunamadı. Yapay zeka sadece başlık ve genel bilgilere dayanarak cevap verebilir."
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Video yüklenirken hata: {ex.Message}", "Hata");
            }
        }

        private async Task SummarizeVideo()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentTranscript))
                {
                    MessageBox.Show("Bu video için altyazı bulunamadı, özet çıkarılamıyor.", "Uyarı");
                    return;
                }

                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "Siz",
                    Content = "Videoyu özetle"
                });

                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "AI",
                    Content = "Video özetleniyor, lütfen bekleyin..."
                });

                string context = $"Video Transkripti:\n{_currentTranscript}";
                string response = await _ollamaService.SendMessageAsync("Aşağıdaki video transkriptini maddeler halinde özetle. Önemli noktaları vurgula:", context, category: OllamaService.ModelCategory.Video);

                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "AI",
                    Content = response
                });
            }
            catch (Exception ex)
            {
                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "Sistem",
                    Content = $"Özetleme hatası: {ex.Message}"
                });
            }
        }

        private void SaveVideo()
        {
            try
            {
                if (CurrentVideo == null) return;

                // Check if already saved
                if (_databaseService.IsVideoSaved(CurrentVideo.VideoId))
                {
                    MessageBox.Show("Bu video zaten kaydedilmiş!", "Bilgi");
                    return;
                }

                // Save to database
                _databaseService.SaveVideoResource(CurrentVideo);
                
                // Reload saved videos list
                LoadSavedVideos();
                
                MessageBox.Show("Video başarıyla kaydedildi!", "Başarılı");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Video kaydedilirken hata: {ex.Message}", "Hata");
            }
        }

        private void LoadSavedVideos()
        {
            try
            {
                SavedVideos.Clear();
                var videos = _databaseService.GetSavedVideos();
                foreach (var video in videos)
                {
                    SavedVideos.Add(video);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydedilen videolar yüklenirken hata: {ex.Message}", "Hata");
            }
        }

        private void EditVideoTitle(VideoResource video)
        {
            try
            {
                if (video == null) return;
                
                // Prompt for new title
                var dialog = new System.Windows.Window
                {
                    Title = "Video İsmini Düzenle",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    ResizeMode = System.Windows.ResizeMode.NoResize
                };
                
                var stackPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new System.Windows.Thickness(20)
                };
                
                var label = new System.Windows.Controls.TextBlock
                {
                    Text = "Yeni video ismi:",
                    Margin = new System.Windows.Thickness(0, 0, 0, 10)
                };
                
                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = video.Title,
                    Margin = new System.Windows.Thickness(0, 0, 0, 10)
                };
                
                var button = new System.Windows.Controls.Button
                {
                    Content = "Kaydet",
                    Width = 100,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };
                
                button.Click += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        _databaseService.UpdateVideoTitle(video.Id, textBox.Text);
                        LoadSavedVideos();
                        dialog.Close();
                    }
                };
                
                stackPanel.Children.Add(label);
                stackPanel.Children.Add(textBox);
                stackPanel.Children.Add(button);
                dialog.Content = stackPanel;
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Video ismi düzenlenirken hata: {ex.Message}", "Hata");
            }
        }

        private void LoadSavedVideo(VideoResource video)
        {
            try
            {
                if (video == null) return;
                
                VideoUrl = video.YouTubeUrl;
                // Fire and forget mechanism for calling async method from synchronous context
                _ = LoadVideo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Video yüklenirken hata: {ex.Message}", "Hata");
            }
        }

        private async System.Threading.Tasks.Task AskQuestion()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentQuestion)) return;

                // Add user message
                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "Siz",
                    Content = CurrentQuestion
                });

                string question = CurrentQuestion;
                CurrentQuestion = "";

                // Create context for AI
                string context = $"Kullanıcı bir YouTube videosu izliyor (Video ID: {CurrentVideo?.VideoId}). ";
                
                if (!string.IsNullOrEmpty(_currentTranscript))
                {
                    context += $"\n\nVideo Transkripti:\n{_currentTranscript}";
                }
                else
                {
                    context += "\nNot: Bu video için altyazı bulunamadı, genel bilgilere dayanarak cevap ver.";
                }
                
                // Get AI response
                string response = await _ollamaService.SendMessageAsync(question, context, category: OllamaService.ModelCategory.Video);

                // Add AI response
                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "AI",
                    Content = response
                });
            }
            catch (Exception ex)
            {
                ChatMessages.Add(new VideoChatMessage
                {
                    Role = "Sistem",
                    Content = $"Hata: {ex.Message}"
                });
            }
        }

        private string ExtractVideoId(string url)
        {
            // YouTube URL patterns
            var patterns = new[]
            {
                @"(?:youtube\.com\/watch\?v=|youtu\.be\/)([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/embed\/([a-zA-Z0-9_-]{11})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
