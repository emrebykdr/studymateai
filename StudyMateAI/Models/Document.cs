using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyMateAI.Models
{
    public class Document : INotifyPropertyChanged
    {
        private int _id;
        private int? _courseId;
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private string _content = string.Empty;
        private string _analysis = string.Empty;
        private DateTime _uploadedAt = DateTime.Now;

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public int? CourseId { get => _courseId; set { _courseId = value; OnPropertyChanged(); } }
        public int? FolderId { get; set; } // New Property
        public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
        public string FilePath { get => _filePath; set { _filePath = value; OnPropertyChanged(); } }
        public string Content { get => _content; set { _content = value; OnPropertyChanged(); } }
        public string Analysis { get => _analysis; set { _analysis = value; OnPropertyChanged(); } } // General/Legacy analysis
        
        public string Summary { get; set; }
        public string Keywords { get; set; } // JSON array or comma separated
        public string UserNotes { get; set; } // User's personal notes

        public DateTime UploadedAt { get => _uploadedAt; set { _uploadedAt = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
