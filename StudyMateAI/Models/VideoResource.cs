using System;

namespace StudyMateAI.Models
{
    public class VideoResource
    {
        public int Id { get; set; }
        public int? CourseId { get; set; }
        public string Title { get; set; } = "";
        public string YouTubeUrl { get; set; } = "";
        public string VideoId { get; set; } = "";
        public int Duration { get; set; } // in seconds
        public string Notes { get; set; } = "";
        public string Transcript { get; set; } = "";
        public DateTime DateAdded { get; set; }
    }
}
