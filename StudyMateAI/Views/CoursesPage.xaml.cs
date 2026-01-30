using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.Views
{
    public partial class CoursesPage : Page
    {
        private readonly DatabaseService _databaseService;
        private Course _selectedCourse;

        public CoursesPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadCourses();
        }

        public CoursesPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            LoadCourses();
        }

        private void LoadCourses()
        {
            try
            {
                var courses = new List<Course>();
                
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "SELECT Id, Name, Code, Credit, MidtermGrade, MidtermPercentage, FinalPercentage FROM Courses ORDER BY Name", 
                        connection);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            courses.Add(new Course
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Credit = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                MidtermGrade = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                                MidtermPercentage = reader.GetInt32(5),
                                FinalPercentage = reader.GetInt32(6)
                            });
                        }
                    }
                }

                CoursesDataGrid.ItemsSource = courses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dersler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoursesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CoursesDataGrid.SelectedItem is Course course)
            {
                _selectedCourse = course;
                UpdateCalculator();
            }
        }

        private void UpdateCalculator()
        {
            if (_selectedCourse == null)
            {
                SelectedCourseText.Text = "Lütfen bir ders seçin";
                MidtermGradeText.Text = "-";
                RequiredFinalText.Text = "-";
                PassStatusText.Text = "";
                GradeScenariosPanel.Children.Clear();
                return;
            }

            SelectedCourseText.Text = _selectedCourse.Name;
            MidtermGradeText.Text = _selectedCourse.MidtermGrade?.ToString("F1") ?? "Girilmemiş";

            if (_selectedCourse.MidtermGrade.HasValue)
            {
                double requiredFinal = _selectedCourse.RequiredFinalGrade;
                RequiredFinalText.Text = requiredFinal.ToString("F1");

                if (requiredFinal <= 0)
                {
                    PassStatusText.Text = "🎉 Tebrikler! Vize notunla zaten geçtin!";
                    PassStatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
                }
                else if (requiredFinal <= 100)
                {
                    PassStatusText.Text = $"Finalden {requiredFinal:F1} alırsan dersi geçersin.";
                    PassStatusText.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    PassStatusText.Text = "⚠️ Maalesef bu dersten geçmek mümkün değil.";
                    PassStatusText.Foreground = new SolidColorBrush(Colors.Red);
                }

                // Grade scenarios
                GradeScenariosPanel.Children.Clear();
                var scenarios = new[]
                {
                    new { Grade = "AA (90+)", Required = CalculateRequiredFinal(90) },
                    new { Grade = "BA (85+)", Required = CalculateRequiredFinal(85) },
                    new { Grade = "BB (80+)", Required = CalculateRequiredFinal(80) },
                    new { Grade = "CB (75+)", Required = CalculateRequiredFinal(75) },
                    new { Grade = "CC (70+)", Required = CalculateRequiredFinal(70) },
                    new { Grade = "DC (60+)", Required = CalculateRequiredFinal(60) },
                    new { Grade = "DD (50+)", Required = CalculateRequiredFinal(50) }
                };

                foreach (var scenario in scenarios)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                    
                    var gradeText = new TextBlock 
                    { 
                        Text = scenario.Grade, 
                        Width = 80, 
                        FontSize = 12 
                    };
                    
                    var requiredText = new TextBlock 
                    { 
                        Text = scenario.Required <= 100 ? $"→ {scenario.Required:F1}" : "→ İmkansız", 
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = scenario.Required <= 100 
                            ? new SolidColorBrush(Colors.LimeGreen) 
                            : new SolidColorBrush(Colors.Red)
                    };
                    
                    panel.Children.Add(gradeText);
                    panel.Children.Add(requiredText);
                    GradeScenariosPanel.Children.Add(panel);
                }
            }
            else
            {
                RequiredFinalText.Text = "-";
                PassStatusText.Text = "Vize notu girilmemiş.";
                PassStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                GradeScenariosPanel.Children.Clear();
            }
        }

        private double CalculateRequiredFinal(double targetGrade)
        {
            if (!_selectedCourse.MidtermGrade.HasValue) return 0;
            
            double midtermContribution = _selectedCourse.MidtermGrade.Value * (_selectedCourse.MidtermPercentage / 100.0);
            double requiredFinal = (targetGrade - midtermContribution) / (_selectedCourse.FinalPercentage / 100.0);
            
            return Math.Round(requiredFinal, 2);
        }

        private void AddCourseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditCourseDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var connection = _databaseService.GetConnection())
                    {
                        connection.Open();
                        var command = new SQLiteCommand(
                            @"INSERT INTO Courses (Name, Code, Credit, MidtermGrade, MidtermPercentage, FinalPercentage) 
                              VALUES (@name, @code, @credit, @midterm, @midtermPct, @finalPct)", 
                            connection);
                        
                        command.Parameters.AddWithValue("@name", dialog.CourseName);
                        command.Parameters.AddWithValue("@code", dialog.CourseCode);
                        command.Parameters.AddWithValue("@credit", dialog.Credit);
                        command.Parameters.AddWithValue("@midterm", dialog.MidtermGrade.HasValue ? (object)dialog.MidtermGrade.Value : DBNull.Value);
                        command.Parameters.AddWithValue("@midtermPct", dialog.MidtermPercentage);
                        command.Parameters.AddWithValue("@finalPct", dialog.FinalPercentage);
                        
                        command.ExecuteNonQuery();
                    }
                    
                    LoadCourses();
                    MessageBox.Show("Ders başarıyla eklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ders eklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int courseId)
            {
                // Find course
                Course? course = null;
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "SELECT Id, Name, Code, Credit, MidtermGrade, MidtermPercentage, FinalPercentage FROM Courses WHERE Id = @id", 
                        connection);
                    command.Parameters.AddWithValue("@id", courseId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            course = new Course
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Credit = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                MidtermGrade = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                                MidtermPercentage = reader.GetInt32(5),
                                FinalPercentage = reader.GetInt32(6)
                            };
                        }
                    }
                }

                if (course != null)
                {
                    var dialog = new AddEditCourseDialog(course);
                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            using (var connection = _databaseService.GetConnection())
                            {
                                connection.Open();
                                var command = new SQLiteCommand(
                                    @"UPDATE Courses SET Name = @name, Code = @code, Credit = @credit, 
                                      MidtermGrade = @midterm, MidtermPercentage = @midtermPct, FinalPercentage = @finalPct 
                                      WHERE Id = @id", 
                                    connection);
                                
                                command.Parameters.AddWithValue("@id", courseId);
                                command.Parameters.AddWithValue("@name", dialog.CourseName);
                                command.Parameters.AddWithValue("@code", dialog.CourseCode);
                                command.Parameters.AddWithValue("@credit", dialog.Credit);
                                command.Parameters.AddWithValue("@midterm", dialog.MidtermGrade.HasValue ? (object)dialog.MidtermGrade.Value : DBNull.Value);
                                command.Parameters.AddWithValue("@midtermPct", dialog.MidtermPercentage);
                                command.Parameters.AddWithValue("@finalPct", dialog.FinalPercentage);
                                
                                command.ExecuteNonQuery();
                            }
                            
                            LoadCourses();
                            MessageBox.Show("Ders başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ders güncellenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void DeleteCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int courseId)
            {
                var result = MessageBox.Show(
                    "Bu dersi silmek istediğinize emin misiniz? İlgili dökümanlar ve chat geçmişi de silinecek.", 
                    "Ders Sil", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var connection = _databaseService.GetConnection())
                        {
                            connection.Open();
                            var command = new SQLiteCommand("DELETE FROM Courses WHERE Id = @id", connection);
                            command.Parameters.AddWithValue("@id", courseId);
                            command.ExecuteNonQuery();
                        }
                        
                        LoadCourses();
                        MessageBox.Show("Ders başarıyla silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ders silinirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
