using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyMateAI.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _aiResponse;
        private bool _isError;

        public int Id { get; set; }
        public int? CourseId { get; set; }
        public string UserMessage { get; set; }
        
        public string AIResponse 
        { 
            get => _aiResponse;
            set
            {
                _aiResponse = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayMessage));
            }
        }

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsUser { get; set; }
        
        public bool IsError 
        { 
            get => _isError;
            set
            {
                _isError = value;
                OnPropertyChanged();
            }
        }

        public string DisplayMessage => IsUser ? UserMessage : AIResponse;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
