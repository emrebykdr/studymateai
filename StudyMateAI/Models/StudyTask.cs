using System;

namespace StudyMateAI.Models
{
    public class StudyTask
    {
        public int Id { get; set; }
        public int StudyPlanId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public double EstimatedHours { get; set; }
        public double CompletedHours { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed
    }
}
