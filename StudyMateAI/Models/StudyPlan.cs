using System;
using System.Collections.ObjectModel;

namespace StudyMateAI.Models
{
    public class StudyPlan
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty; // e.g., "Mathematics"
        public string GoalDescription { get; set; } = string.Empty; // e.g., "Final Exam"
        public double TotalTargetHours { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);
        public bool IsActive { get; set; } = true;
        
        // Non-DB property for UI convenience
        public ObservableCollection<StudyTask> Tasks { get; set; } = new ObservableCollection<StudyTask>();
    }
}
