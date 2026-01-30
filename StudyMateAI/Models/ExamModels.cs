using System.Collections.Generic;

namespace StudyMateAI.Models
{
    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectAnswer { get; set; } // Index 0-3
        public string Explanation { get; set; }
        public string ModelAnswer { get; set; } // For Open Ended

        // Runtime state
        public int SelectedOptionIndex { get; set; } = -1;
        public string UserAnswerText { get; set; } = "";
        public double Score { get; set; } // 0-100
        public string Feedback { get; set; } = "";
        public bool IsEvaluated { get; set; }
    }

    public class ExamReport
    {
        public double Score { get; set; }
        public string OverallAssessment { get; set; }
        public List<string> WeakTopics { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}
