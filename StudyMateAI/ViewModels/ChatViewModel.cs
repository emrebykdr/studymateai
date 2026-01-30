using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StudyMateAI.Models;
using StudyMateAI.Services;
using StudyMateAI.Helpers;

namespace StudyMateAI.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly OllamaService _ollamaService;
        private readonly DatabaseService _databaseService;
        private string _userInput;
        private bool _isLoading;
        private bool _isOllamaAvailable;

        public ObservableCollection<ChatMessage> Messages { get; set; }

        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsOllamaAvailable
        {
            get => _isOllamaAvailable;
            set
            {
                _isOllamaAvailable = value;
                OnPropertyChanged();
            }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public ChatViewModel()
        {
            _ollamaService = new OllamaService();
            _databaseService = new DatabaseService();
            Messages = new ObservableCollection<ChatMessage>();

            SendMessageCommand = new RelayCommandAsync(SendMessageAsync, () => !string.IsNullOrWhiteSpace(UserInput) && !IsLoading);
            ClearHistoryCommand = new RelayCommand(_ => ClearHistory());

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            IsOllamaAvailable = await _ollamaService.IsOllamaRunningAsync();
            
            if (!IsOllamaAvailable)
            {
                Messages.Add(new ChatMessage
                {
                    UserMessage = string.Empty,
                    AIResponse = "⚠️ Ollama servisi çalışmıyor. Lütfen Ollama'yı başlatın ve tekrar deneyin.",
                    Timestamp = DateTime.Now,
                    IsError = true
                });
            }
            else
            {
                // Load chat history from database
                var history = _databaseService.GetChatHistory();
                foreach (var message in history)
                {
                    Messages.Add(message);
                }

                if (Messages.Count == 0)
                {
                    Messages.Add(new ChatMessage
                    {
                        UserMessage = string.Empty,
                        AIResponse = "👋 Merhaba! Ben StudyMate AI asistanınızım. Size nasıl yardımcı olabilirim?",
                        Timestamp = DateTime.Now
                    });
                }
            }
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsLoading)
                return;

            var userMessage = UserInput.Trim();
            UserInput = string.Empty;

            // Add user message
            var chatMessage = new ChatMessage
            {
                UserMessage = userMessage,
                Timestamp = DateTime.Now,
                IsUser = true
            };
            Messages.Add(chatMessage);
            
            // Add initial empty AI message
            var aiMessage = new ChatMessage
            {
                UserMessage = string.Empty,
                AIResponse = "",
                Timestamp = DateTime.Now,
                IsUser = false
            };
            Messages.Add(aiMessage);

            IsLoading = true;

            try
            {
                // Stream AI response
                await foreach (var chunk in _ollamaService.SendMessageStreamAsync(userMessage, category: OllamaService.ModelCategory.Chat))
                {
                    aiMessage.AIResponse += chunk;
                }
                
                // Save to database
                _databaseService.SaveChatMessage(null, userMessage, aiMessage.AIResponse);
            }
            catch (Exception ex)
            {
                aiMessage.AIResponse = $"❌ Hata: {ex.Message}";
                aiMessage.IsError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearHistory()
        {
            var result = MessageBox.Show(
                "Tüm sohbet geçmişi silinecek. Emin misiniz?",
                "Geçmişi Temizle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Messages.Clear();
                _databaseService.ClearChatHistory();
                
                Messages.Add(new ChatMessage
                {
                    UserMessage = string.Empty,
                    AIResponse = "👋 Sohbet geçmişi temizlendi. Size nasıl yardımcı olabilirim?",
                    Timestamp = DateTime.Now
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
