using System;

namespace StudyMateAI.Models
{
    public class DailySchedule
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int StudyPlanId { get; set; }
        public int PlannedMinutes { get; set; }
        public bool IsCompleted { get; set; }
        public string TaskTopic { get; set; } = ""; // Store task topic/name
        
        // UI Helpers
        public string DayName => Date.ToString("dddd");
        public string DateDisplay => Date.ToString("dd.MM.yyyy");
        public string PlanName { get; set; } = "";
        public string HoursDisplay => PlannedMinutes > 0 ? $"{PlannedMinutes / 60.0:F1} saat" : "-";
        public string DisplayName => !string.IsNullOrEmpty(TaskTopic) ? TaskTopic : PlanName; // Show task topic if available
    }
}
