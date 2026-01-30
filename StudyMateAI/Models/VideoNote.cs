using System;

namespace StudyMateAI.Models
{
    public class VideoNote
    {
        public int Id { get; set; }
        public int VideoResourceId { get; set; }
        public int Timestamp { get; set; } // in seconds
        public string Note { get; set; } = "";
        public DateTime CreatedDate { get; set; }
    }
}
