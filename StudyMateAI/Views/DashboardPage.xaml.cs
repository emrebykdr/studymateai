using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.Views
{
    public partial class DashboardPage : Page
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;

        public DashboardPage()
        {
            InitializeComponent();
            
            try
            {
                _databaseService = new DatabaseService();
                _ollamaService = new OllamaService();
                
                // UI elementleri hazır olduğunda yükle
                this.Loaded += DashboardPage_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlatma hatası: {ex.Message}\n\nDetay: {ex.StackTrace}", 
                    "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public DashboardPage(DatabaseService databaseService, OllamaService ollamaService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _ollamaService = ollamaService;
            
            this.Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
            CheckOllamaStatus();
        }

        private async void CheckOllamaStatus()
        {
            if (_ollamaService == null)
            {
                OllamaStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                OllamaStatusText.Text = "Ollama servisi başlatılamadı!";
                return;
            }

            bool isRunning = await _ollamaService.IsOllamaRunningAsync();
            
            if (isRunning)
            {
                OllamaStatusIcon.Foreground = new SolidColorBrush(Colors.LimeGreen);
                OllamaStatusText.Text = "Ollama çalışıyor ✓";
            }
            else
            {
                OllamaStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                OllamaStatusText.Text = "Ollama çalışmıyor! Lütfen Ollama'yı başlatın.";
            }
        }

        private void LoadDashboardData()
        {
            if (_databaseService == null)
            {
                MessageBox.Show("Veritabanı servisi başlatılamadı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using ( var connection = _databaseService.GetConnection())
                {
                    connection.Open();

                    // Get total courses
                    var coursesCommand = new SQLiteCommand("SELECT COUNT(*) FROM Courses", connection);
                    int totalCourses = Convert.ToInt32(coursesCommand.ExecuteScalar());
                    TotalCoursesText.Text = totalCourses.ToString();

                    // Get average grade
                    var avgCommand = new SQLiteCommand(
                        "SELECT AVG(MidtermGrade) FROM Courses WHERE MidtermGrade IS NOT NULL", 
                        connection);
                    var avgResult = avgCommand.ExecuteScalar();
                    double avgGrade = avgResult != DBNull.Value ? Convert.ToDouble(avgResult) : 0;
                    AverageGradeText.Text = avgGrade.ToString("F1");

                    // Get total documents
                    var docsCommand = new SQLiteCommand("SELECT COUNT(*) FROM Documents", connection);
                    int totalDocs = Convert.ToInt32(docsCommand.ExecuteScalar());
                    TotalDocumentsText.Text = totalDocs.ToString();

                    // Get active courses
                    var courses = new List<Course>();
                    var selectCommand = new SQLiteCommand(
                        "SELECT Id, Name, Code, Credit, MidtermGrade, MidtermPercentage, FinalPercentage FROM Courses ORDER BY CreatedAt DESC LIMIT 5", 
                        connection);
                    
                    using (var reader = selectCommand.ExecuteReader())
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

                    if (courses.Any())
                    {
                        ActiveCoursesListView.ItemsSource = courses;
                        NoCoursesText.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ActiveCoursesListView.Visibility = Visibility.Collapsed;
                        NoCoursesText.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
