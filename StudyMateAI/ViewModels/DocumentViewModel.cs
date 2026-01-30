using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StudyMateAI.Helpers;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.ViewModels
{
    public class DocumentViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;
        
        private Document? _selectedDocument;
        private Folder? _selectedFolder;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private string _analysisResult = string.Empty;
        private string _summary = string.Empty;
        private ObservableCollection<string> _keywords;
        private string _mindMapHtml = string.Empty;
        private bool _isPdfViewerVisible;
        private string _pdfSource = "about:blank";

        public ObservableCollection<Document> Documents { get; set; }
        public ObservableCollection<Folder> Folders { get; set; }
        
        public Document? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                if (_selectedDocument == value) return;
                _selectedDocument = value;
                OnPropertyChanged();
                LoadDocumentContent();
                // Reset viewer if document changes
                IsPdfViewerVisible = false;
                PdfSource = "about:blank";
            }
        }
        
        public Folder? SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (_selectedFolder == value) return;
                _selectedFolder = value;
                OnPropertyChanged();
                FilterDocuments();
            }
        }

        public bool IsPdfViewerVisible
        {
            get => _isPdfViewerVisible;
            set
            {
                if (_isPdfViewerVisible == value) return;
                _isPdfViewerVisible = value;
                OnPropertyChanged();
            }
        }

        public string PdfSource
        {
            get => _pdfSource;
            set
            {
                if (_pdfSource == value) return;
                _pdfSource = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                FilterDocuments();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string Summary
        {
            get => _summary;
            set { _summary = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Keywords
        {
            get => _keywords;
            set { _keywords = value; OnPropertyChanged(); }
        }

        public string MindMapHtml
        {
            get => _mindMapHtml;
            set { _mindMapHtml = value; OnPropertyChanged(); }
        }

        public string AnalysisResult
        {
            get => _analysisResult;
            set
            {
                if (_analysisResult == value) return;
                _analysisResult = value;
                OnPropertyChanged();
            }
        }

        public ICommand UploadDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }
        public ICommand AnalyzeDocumentCommand { get; }
        public ICommand ViewPdfCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddFolderCommand { get; }

        public ICommand DeleteFolderCommand { get; }
        
        // New Commands
        public ICommand ViewSummaryPdfCommand { get; }
        public ICommand SaveSummaryPdfCommand { get; }
        public ICommand ViewAnalysisPdfCommand { get; }
        public ICommand SaveAnalysisPdfCommand { get; }
        public ICommand SaveUserNotesCommand { get; }
        public ICommand ViewUserNotesPdfCommand { get; }
        public ICommand SaveUserNotesPdfCommand { get; }

        public string UserNotes
        {
            get => _userNotes;
            set { _userNotes = value; OnPropertyChanged(); }
        }
        private string _userNotes = string.Empty;

        public DocumentViewModel()
        {
            _databaseService = new DatabaseService();
            _ollamaService = new OllamaService();

            _ollamaService = new OllamaService();
            _keywords = new ObservableCollection<string>();

            Documents = new ObservableCollection<Document>();
            Folders = new ObservableCollection<Folder>();

            UploadDocumentCommand = new RelayCommand(_ => UploadDocument());
            DeleteDocumentCommand = new RelayCommand(_ => DeleteDocument(), _ => SelectedDocument != null);
            AnalyzeDocumentCommand = new RelayCommandAsync(AnalyzeDocument, () => SelectedDocument != null && !IsLoading);
            ViewPdfCommand = new RelayCommand(_ => TogglePdfViewer(), _ => SelectedDocument != null && (SelectedDocument.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)));
            RefreshCommand = new RelayCommand(_ => LoadDocuments()); // Keep simple refresh
            AddFolderCommand = new RelayCommand(_ => AddFolder());

            DeleteFolderCommand = new RelayCommand(_ => DeleteFolder(), _ => SelectedFolder != null && SelectedFolder.Id > 0);

            // Init New Commands
            ViewSummaryPdfCommand = new RelayCommand(_ => GenerateAndShowPdf("Özet", Summary), _ => !string.IsNullOrEmpty(Summary));
            SaveSummaryPdfCommand = new RelayCommand(_ => SavePdf("Özet", Summary), _ => !string.IsNullOrEmpty(Summary));
            
            ViewAnalysisPdfCommand = new RelayCommand(_ => GenerateAndShowPdf("Genel Analiz", AnalysisResult), _ => !string.IsNullOrEmpty(AnalysisResult));
            SaveAnalysisPdfCommand = new RelayCommand(_ => SavePdf("Genel Analiz", AnalysisResult), _ => !string.IsNullOrEmpty(AnalysisResult));

            // For Mind Map, we pass the text code for now, ensuring it's not empty
            // MindMap commands removed
            // User Notes Command
            SaveUserNotesCommand = new RelayCommand(_ => SaveUserNotes(), _ => SelectedDocument != null);
            ViewUserNotesPdfCommand = new RelayCommand(_ => GenerateAndShowPdf("Notlarım", UserNotes), _ => !string.IsNullOrEmpty(UserNotes));
            SaveUserNotesPdfCommand = new RelayCommand(_ => SavePdf("Notlarım", UserNotes), _ => !string.IsNullOrEmpty(UserNotes));

            LoadFolders();
            FilterDocuments(); // Initial load
        }

        private void LoadFolders()
        {
            Folders.Clear();
            
            // Default "System" Folders
            Folders.Add(new Folder { Id = -1, Name = "Tüm Dökümanlar" });
            Folders.Add(new Folder { Id = 0, Name = "Atanmamış" });

            var dbFolders = _databaseService.GetAllFolders();
            foreach (var f in dbFolders)
            {
                Folders.Add(f);
            }

            // Default selection: All Documents
            SelectedFolder = Folders.FirstOrDefault();
        }

        private void AddFolder()
        {
            var dialog = new StudyMateAI.Views.FolderCreationDialog();
            if (dialog.ShowDialog() == true)
            {
                var newFolder = new Folder { Name = dialog.FolderName };
                _databaseService.AddFolder(newFolder);
                LoadFolders();
                // Select the new folder? Optional.
                SelectedFolder = Folders.LastOrDefault(); // Assuming added at end
            }
        }

        private void DeleteFolder()
        {
             if (SelectedFolder == null || SelectedFolder.Id <= 0) return;

             var result = MessageBox.Show(
                 $"'{SelectedFolder.Name}' klasörünü silmek istediğinize emin misiniz? (İçindeki dökümanlar 'Atanmamış' olarak kalacaktır.)",
                 "Klasör Sil",
                 MessageBoxButton.YesNo,
                 MessageBoxImage.Warning);

             if (result == MessageBoxResult.Yes)
             {
                 _databaseService.DeleteFolder(SelectedFolder.Id);
                 LoadFolders();
                 SelectedFolder = Folders.FirstOrDefault(); // Reset to All
             }
        }

        private void UploadDocument()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Supported Files|*.pdf;*.docx;*.txt|PDF Files|*.pdf|Word Documents|*.docx|Text Files|*.txt|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Determine Folder ID
                int? targetFolderId = null;
                if (SelectedFolder != null && SelectedFolder.Id > 0)
                {
                    targetFolderId = SelectedFolder.Id;
                }

                int successCount = 0;
                foreach (var filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        string content = DocumentHelper.ExtractTextFromFile(filePath);

                        var document = new Document
                        {
                            FileName = fileName,
                            FilePath = filePath,
                            Content = content,
                            UploadedAt = DateTime.Now,
                            FolderId = targetFolderId,
                            CourseId = null // Assignment removed
                        };

                        _databaseService.AddDocument(document);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        var name = Path.GetFileName(filePath);
                        MessageBox.Show($"'{name}' yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (successCount > 0)
                {
                    FilterDocuments(); // Reload to show new docs
                    
                    string msg = $"{successCount} döküman başarıyla yüklendi.";
                    if (targetFolderId.HasValue)
                        msg += $"\nKlasör: {SelectedFolder?.Name}";
                    
                    MessageBox.Show(msg, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteDocument()
        {
            if (SelectedDocument == null) return;

            var result = MessageBox.Show(
                $"'{SelectedDocument.FileName}' dosyasını silmek istediğinize emin misiniz?",
                "Dosya Sil",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeleteDocument(SelectedDocument.Id);
                Documents.Remove(SelectedDocument);
                SelectedDocument = null;
                AnalysisResult = string.Empty;
                // If filtering, removing from observable collection is enough, but FilterDocuments refreshes all so maybe clearer to call that
                // But efficient Remove is better for UX
            }
        }

        private void LoadDocuments()
        {
             FilterDocuments();
        }

        private void FilterDocuments()
        {
            var allDocs = _databaseService.GetAllDocuments().AsEnumerable();

            // Filter by Folder
            if (SelectedFolder != null)
            {
                if (SelectedFolder.Id == -1) 
                {
                    // All Documents
                }
                else if (SelectedFolder.Id == 0)
                {
                    // Unassigned (FolderId is null)
                    allDocs = allDocs.Where(d => d.FolderId == null);
                }
                else
                {
                    // Specific Folder
                    allDocs = allDocs.Where(d => d.FolderId == SelectedFolder.Id);
                }
            }

            // Filter by Search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                allDocs = allDocs.Where(d => d.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            var results = allDocs.ToList();
            
            // Preserve selection if possible
            int? currentId = SelectedDocument?.Id;

            Documents.Clear();
            foreach (var doc in results)
            {
                Documents.Add(doc);
            }

            if (currentId.HasValue)
            {
                SelectedDocument = Documents.FirstOrDefault(d => d.Id == currentId.Value);
            }
        }

        private async Task AnalyzeDocument()
        {
            if (SelectedDocument == null) return;

             // 1. Validate Content
            if (string.IsNullOrWhiteSpace(SelectedDocument.Content) || 
                SelectedDocument.Content.StartsWith("[HATA]") || 
                SelectedDocument.Content.StartsWith("[UYARI]"))
            {
                 // Try Refresh logic if needed... or simplified
                  AnalysisResult = "⚠️ Bu döküman analiz edilemedi. İçerik okunamadı.";
                  return;
            }

            IsLoading = true;
            AnalysisResult = "AI analiz ediyor (Özet, Anahtar Kelimeler ve Zihin Haritası)...";

            try
            {
                // Parallel Execution
                var summaryTask = _ollamaService.GenerateSummaryAsync(SelectedDocument.Content);
                var keywordsTask = _ollamaService.ExtractKeywordsAsync(SelectedDocument.Content);
                // var mindMapTask = _ollamaService.GenerateMindMapAsync(SelectedDocument.Content); // Removed
                var generalTask = _ollamaService.GenerateTopicExplanationAsync(SelectedDocument.Content);

                await Task.WhenAll(summaryTask, keywordsTask, generalTask);

                // Update Model
                SelectedDocument.Summary = summaryTask.Result;
                SelectedDocument.Keywords = keywordsTask.Result;
                // SelectedDocument.MindMapData = mindMapTask.Result; // Removed
                SelectedDocument.Analysis = generalTask.Result;

                // Update DB only for AI fields
                _databaseService.UpdateDocumentDetailedAnalysis(SelectedDocument.Id, SelectedDocument.Summary, SelectedDocument.Keywords);
                _databaseService.UpdateDocumentAnalysis(SelectedDocument.Id, SelectedDocument.Analysis); 

                // Update UI
                Summary = SelectedDocument.Summary;
                AnalysisResult = SelectedDocument.Analysis;
                
                Keywords.Clear();
                if (!string.IsNullOrEmpty(SelectedDocument.Keywords))
                {
                    foreach(var k in SelectedDocument.Keywords.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        Keywords.Add(k.Trim());
                }

                UpdateUserNotes(SelectedDocument.UserNotes);
            }
            catch (Exception ex)
            {
                AnalysisResult = $"❌ Analiz hatası: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateUserNotes(string notes)
        {
            UserNotes = notes ?? string.Empty;
        }

        private void SaveUserNotes()
        {
            if (SelectedDocument != null)
            {
                SelectedDocument.UserNotes = UserNotes;
                _databaseService.UpdateDocumentNotes(SelectedDocument.Id, UserNotes);
                MessageBox.Show("Notlarınız kaydedildi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadDocumentContent()
        {
            if (SelectedDocument != null)
            {
                AnalysisResult = SelectedDocument.Analysis ?? string.Empty;
                Summary = SelectedDocument.Summary ?? string.Empty;
                
                Keywords.Clear();
                if (!string.IsNullOrEmpty(SelectedDocument.Keywords))
                {
                    foreach(var k in SelectedDocument.Keywords.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        Keywords.Add(k.Trim());
                }

                UpdateUserNotes(SelectedDocument.UserNotes);
            }
            else
            {
                AnalysisResult = string.Empty;
                Summary = string.Empty;
                Keywords.Clear();
                UserNotes = string.Empty;
            }
        }

        private void TogglePdfViewer()
        {
            if (SelectedDocument == null || string.IsNullOrEmpty(SelectedDocument.FilePath)) return;
            
            if (IsPdfViewerVisible)
            {
                IsPdfViewerVisible = false;
                PdfSource = "about:blank";
            }
            else
            {
                PdfSource = SelectedDocument.FilePath;
                IsPdfViewerVisible = true;
            }
        }

        private void GenerateAndShowPdf(string title, string content)
        {
            try 
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"StudyMate_View_{Guid.NewGuid()}.pdf");
                // Prepend title to content for context if needed, or let Helper handle header
                PdfGenerationHelper.GeneratePdf(tempPath, title, content);
                
                PdfSource = tempPath;
                IsPdfViewerVisible = true;
            }
            catch (Exception ex)
            {
                 MessageBox.Show($"PDF oluşturulurken hata oluştu: {ex.Message}", "Hata");
            }
        }

        private void SavePdf(string title, string content)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF Dosyası|*.pdf",
                FileName = $"{Path.GetFileNameWithoutExtension(SelectedDocument?.FileName)}_{title}.pdf"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                 try
                 {
                     PdfGenerationHelper.GeneratePdf(saveDialog.FileName, title, content);
                     
                     // Optional: Add to DB if user wants (Prompt or Auto)
                     // Implementing "Save to Documents" as explicitly adding it to the system
                     var result = MessageBox.Show("PDF kaydedildi. Bu dosyayı döküman listenize eklemek ister misiniz?", "Döküman Ekle", MessageBoxButton.YesNo, MessageBoxImage.Question);
                     if (result == MessageBoxResult.Yes)
                     {
                         var newDoc = new Document
                         {
                             FileName = Path.GetFileName(saveDialog.FileName),
                             FilePath = saveDialog.FileName,
                             Content = content, // Or re-extract
                             UploadedAt = DateTime.Now,
                             FolderId = SelectedFolder?.Id > 0 ? SelectedFolder.Id : null
                         };
                         _databaseService.AddDocument(newDoc);
                         FilterDocuments(); // Refresh list
                     }
                 }
                 catch (Exception ex)
                 {
                     MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata");
                 }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
