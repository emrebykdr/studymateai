using System;

namespace StudyMateAI.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int Credit { get; set; }
        public double? MidtermGrade { get; set; }
        public int MidtermPercentage { get; set; } = 40;
        public int FinalPercentage { get; set; } = 60;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Calculated property
        public double RequiredFinalGrade
        {
            get
            {
                if (!MidtermGrade.HasValue) return 50.0;
                
                // Formula: (50 - (vize * vize%)) / final%
                double midtermContribution = MidtermGrade.Value * (MidtermPercentage / 100.0);
                double requiredFinal = (50.0 - midtermContribution) / (FinalPercentage / 100.0);
                
                return Math.Round(requiredFinal, 2);
            }
        }

        public bool CanPass => MidtermGrade.HasValue && RequiredFinalGrade <= 100;
    }
}
