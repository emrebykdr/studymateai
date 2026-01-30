using System;
using System.Windows;
using StudyMateAI.Models;

namespace StudyMateAI.Views
{
    public partial class AddEditCourseDialog : Window
    {
        public string CourseName { get; private set; }
        public string CourseCode { get; private set; }
        public int Credit { get; private set; }
        public double? MidtermGrade { get; private set; }
        public int MidtermPercentage { get; private set; } = 40;
        public int FinalPercentage { get; private set; } = 60;

        public AddEditCourseDialog(Course course = null)
        {
            InitializeComponent();

            if (course != null)
            {
                // Edit mode
                Title = "Ders Düzenle";
                CourseNameTextBox.Text = course.Name;
                CourseCodeTextBox.Text = course.Code;
                CreditTextBox.Text = course.Credit.ToString();
                MidtermGradeTextBox.Text = course.MidtermGrade?.ToString();
                MidtermPercentageTextBox.Text = course.MidtermPercentage.ToString();
                FinalPercentageTextBox.Text = course.FinalPercentage.ToString();
            }
            else
            {
                // Add mode
                Title = "Yeni Ders Ekle";
                MidtermPercentageTextBox.Text = "40";
                FinalPercentageTextBox.Text = "60";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(CourseNameTextBox.Text))
            {
                MessageBox.Show("Ders adı boş olamaz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CourseName = CourseNameTextBox.Text.Trim();
            CourseCode = CourseCodeTextBox.Text.Trim();

            if (int.TryParse(CreditTextBox.Text, out int credit))
            {
                Credit = credit;
            }

            if (!string.IsNullOrWhiteSpace(MidtermGradeTextBox.Text))
            {
                if (double.TryParse(MidtermGradeTextBox.Text, out double midterm))
                {
                    if (midterm >= 0 && midterm <= 100)
                    {
                        MidtermGrade = midterm;
                    }
                    else
                    {
                        MessageBox.Show("Vize notu 0-100 arasında olmalı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            if (int.TryParse(MidtermPercentageTextBox.Text, out int midtermPct))
            {
                MidtermPercentage = midtermPct;
            }

            if (int.TryParse(FinalPercentageTextBox.Text, out int finalPct))
            {
                FinalPercentage = finalPct;
            }

            if (MidtermPercentage + FinalPercentage != 100)
            {
                MessageBox.Show("Vize ve Final yüzdeleri toplamı 100 olmalı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
