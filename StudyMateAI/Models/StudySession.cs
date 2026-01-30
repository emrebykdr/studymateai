using System;

namespace StudyMateAI.Models
{
    public class StudySession
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int Duration { get; set; } // in minutes
        public string Topic { get; set; }
        public DateTime SessionDate { get; set; } = DateTime.Now;
    }
}
