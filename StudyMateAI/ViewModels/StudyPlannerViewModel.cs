using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using StudyMateAI.Helpers;
using StudyMateAI.Models;
using StudyMateAI.Services;

namespace StudyMateAI.ViewModels
{
    public class StudyPlannerViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;
        private DispatcherTimer _timer;
        
        private StudyPlan? _selectedPlan;
        private StudyTask? _selectedTask;
        private string _inputTopics = string.Empty;
        private bool _isGenerating;
        private TimeSpan _currentTime;
        private bool _isTimerRunning;

        public ObservableCollection<StudyPlan> Plans { get; set; }
        public ObservableCollection<StudyTask> CurrentPlanTasks { get; set; }
        public ObservableCollection<DailySchedule> WeeklySchedule { get; set; }

        public StudyPlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (_selectedPlan == value) return;
                _selectedPlan = value;
                OnPropertyChanged();
                LoadTasksForPlan();
            }
        }

        public StudyTask? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_selectedTask == value) return;
                _selectedTask = value;
                OnPropertyChanged();
            }
        }

        public string InputTopics
        {
            get => _inputTopics;
            set { _inputTopics = value; OnPropertyChanged(); }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set { _isGenerating = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateButtonText)); CommandManager.InvalidateRequerySuggested(); }
        }
        public string GenerateButtonText => IsGenerating ? "oluşturuluyor..." : "AI ile Oluştur";
        
        public string TimerDisplay => _currentTime.ToString(@"hh\:mm\:ss");
        
        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set { _isTimerRunning = value; OnPropertyChanged(); }
        }

        public ICommand GeneratePlanCommand { get; }
        public ICommand ToggleTimerCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand AddManualTimeCommand { get; }
        public ICommand DeletePlanCommand { get; }
        public ICommand CompletePlanCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DistributeToWeekCommand { get; }
        public ICommand DeleteScheduleCommand { get; }

        public StudyPlannerViewModel()
        {
            _databaseService = new DatabaseService();
            _ollamaService = new OllamaService();
            
            Plans = new ObservableCollection<StudyPlan>();
            CurrentPlanTasks = new ObservableCollection<StudyTask>();
            WeeklySchedule = new ObservableCollection<DailySchedule>();
            
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            GeneratePlanCommand = new RelayCommandAsync(GeneratePlan, () => !string.IsNullOrWhiteSpace(InputTopics) && !IsGenerating);
            AddManualPlanCommand = new RelayCommand(_ => AddManualPlan());
            OpenAddTaskDialogCommand = new RelayCommand(_ => OpenAddTaskDialog());
            ToggleTimerCommand = new RelayCommand(_ => ToggleTimer());
            SaveSessionCommand = new RelayCommand(_ => SaveSession(), _ => _currentTime.TotalMinutes >= 1 && SelectedTask != null);
            CompleteTaskCommand = new RelayCommand(p => MarkTaskComplete(p as StudyTask), _ => SelectedTask != null || true); // Always allow if parameter provided? Simplifying canExecute for now or just true
            DeleteTaskCommand = new RelayCommand(p => DeleteTask(p as StudyTask), _ => SelectedTask != null || true);
            DeletePlanCommand = new RelayCommand(p => DeletePlan(p as StudyPlan), _ => SelectedPlan != null || true);
            CompletePlanCommand = new RelayCommand(p => CompletePlan(p as StudyPlan), _ => SelectedPlan != null || true);
            DistributeToWeekCommand = new RelayCommandAsync(DistributeToWeek, () => SelectedPlan != null && !IsGenerating);
            DeleteScheduleCommand = new RelayCommand(DeleteSchedule);

            LoadPlans();
            LoadWeeklySchedule();
        }

        public ICommand OpenAddTaskDialogCommand { get; }
        public ICommand AddManualPlanCommand { get; }

        private void AddManualPlan()
        {
            if (string.IsNullOrWhiteSpace(InputTopics))
            {
                MessageBox.Show("Lütfen plan adı veya konu listesi giriniz.", "Uyarı");
                return;
            }

            try
            {
                // Create Plan Entry
                var newPlan = new StudyPlan
                {
                    Subject = InputTopics.Split(',').FirstOrDefault()?.Trim() ?? "Yeni Çalışma Planı",
                    GoalDescription = "Manuel Oluşturuldu",
                    StartDate = DateTime.Now,
                    IsActive = true,
                    TotalTargetHours = 0
                };

                int planId = _databaseService.AddStudyPlan(newPlan);
                newPlan.Id = planId;

                // Add topics as tasks if comma separated
                var topics = InputTopics.Split(',');
                foreach (var topic in topics)
                {
                    string t = topic.Trim();
                    if (!string.IsNullOrEmpty(t))
                    {
                        var task = new StudyTask
                        {
                            StudyPlanId = planId,
                            Topic = t,
                            EstimatedHours = 1.0, // Default to 1 hour
                            Status = "Pending"
                        };
                        _databaseService.AddStudyTask(task);
                        newPlan.TotalTargetHours += 1.0;
                    }
                }
                
                // Update Plan Total Hours in DB ?? (Ideally updates automatically or we should update it)
                // Not crucial for now.

                LoadPlans();
                SelectedPlan = Plans.FirstOrDefault(p => p.Id == planId);
                InputTopics = ""; // Reset
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plan oluşturulamadı: {ex.Message}", "Hata");
            }
        }

        private void OpenAddTaskDialog()
        {
            if (SelectedPlan == null)
            {
                 MessageBox.Show("Lütfen önce sol taraftan bir plan seçiniz.", "Uyarı");
                 return;
            }

            var dialog = new StudyMateAI.Views.AddTaskDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var task = new StudyTask
                    {
                        StudyPlanId = SelectedPlan.Id,
                        Topic = dialog.Topic,
                        EstimatedHours = dialog.Hours,
                        Status = "Pending"
                    };
                    _databaseService.AddStudyTask(task);
                    LoadTasksForPlan(); // Refresh list
                }
                catch (Exception ex)
                {
                     MessageBox.Show($"Görev eklenirken hata: {ex.Message}", "Hata");
                }
            }
        }

        private void LoadPlans()
        {
            Plans.Clear();
            var dbPlans = _databaseService.GetActiveStudyPlans();
            foreach (var p in dbPlans) Plans.Add(p);
            
            if (Plans.Any())
            {
                 if (SelectedPlan == null || !Plans.Any(p => p.Id == SelectedPlan.Id))
                     SelectedPlan = Plans.First();
            }
        }

        private void LoadTasksForPlan()
        {
            // Preserve selection if possible
            int? selectedId = SelectedTask?.Id;
            
            CurrentPlanTasks.Clear();
            if (SelectedPlan == null) return;

            var tasks = _databaseService.GetTasksForPlan(SelectedPlan.Id);
            foreach (var t in tasks) CurrentPlanTasks.Add(t);
            
            if (selectedId.HasValue)
            {
                SelectedTask = CurrentPlanTasks.FirstOrDefault(t => t.Id == selectedId.Value);
            }
        }

        private async Task GeneratePlan()
        {
            IsGenerating = true;
            try
            {
                // Create Plan Entry
                var newPlan = new StudyPlan
                {
                    Subject = InputTopics.Split(',').FirstOrDefault()?.Trim() ?? "Yeni Çalışma Planı",
                    GoalDescription = "AI Oluşturdu",
                    StartDate = DateTime.Now,
                    IsActive = true
                };

                // Call AI
                string response = await _ollamaService.GenerateStudyPlanAsync(InputTopics);
                
                // Clean Response (remove markdown)
                response = response.Replace("```json", "").Replace("```", "").Trim();
                
                var tasksData = JsonConvert.DeserializeObject<dynamic[]>(response);
                
                if (tasksData != null)
                {
                    // Save Plan Headers
                    double totalHours = 0;
                    foreach(var t in tasksData)
                    {
                        totalHours += (double)(t.EstimatedHours ?? 1.0);
                    }
                    newPlan.TotalTargetHours = totalHours;
                    
                    int planId = _databaseService.AddStudyPlan(newPlan);
                    newPlan.Id = planId;

                    // Save Tasks
                    foreach(var t in tasksData)
                    {
                        var task = new StudyTask
                        {
                            StudyPlanId = planId,
                            Topic = (string)t.Topic,
                            EstimatedHours = (double)(t.EstimatedHours ?? 1.0),
                            Status = "Pending"
                        };
                        _databaseService.AddStudyTask(task);
                    }

                    LoadPlans();
                    SelectedPlan = Plans.FirstOrDefault(p => p.Id == planId);
                    InputTopics = ""; // Reset
                    MessageBox.Show("Çalışma planı oluşturuldu!", "Başarılı");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Plan oluşturulamadı: {ex.Message} \n\nAI Yanıtı düzgün ayrıştırılamadı.", "Hata");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _currentTime = _currentTime.Add(TimeSpan.FromSeconds(1));
            OnPropertyChanged(nameof(TimerDisplay));
        }

        private void ToggleTimer()
        {
            if (IsTimerRunning)
            {
                _timer.Stop();
                IsTimerRunning = false;
            }
            else
            {
                _timer.Start();
                IsTimerRunning = true;
            }
        }

        private void SaveSession()
        {
            if (SelectedTask == null) return;
            
            double minutes = _currentTime.TotalMinutes;
            double hours = minutes / 60.0;

            // Update Task Progress
            _databaseService.UpdateTaskProgress(SelectedTask.Id, hours);
            // SelectedTask.CompletedHours += hours; // Don't manually update, reload keeps it clean
            
            // Refresh
            LoadTasksForPlan();

            // Reset Timer
            _timer.Stop();
            IsTimerRunning = false;
            _currentTime = TimeSpan.Zero;
            OnPropertyChanged(nameof(TimerDisplay));

            MessageBox.Show($"Süre kaydedildi: {minutes:F0} dakika.", "Kaydedildi");
        }

        private void MarkTaskComplete(StudyTask? task = null)
        {
            var targetTask = task ?? SelectedTask;
            if (targetTask == null) return;
            
            _databaseService.UpdateTaskStatus(targetTask.Id, "Completed");
            
            // Visual feedback?
            // MessageBox.Show($"{targetTask.Topic} tamamlandı!", "Tebrikler"); // Optional feedback
            
            LoadTasksForPlan(); // Refresh
        }

        private void DeleteTask(StudyTask? task = null)
        {
            var targetTask = task ?? SelectedTask;
            if (targetTask == null) return;
            
            var result = MessageBox.Show($"'{targetTask.Topic}' görevini silmek istediğinize emin misiniz?", 
                "Görevi Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeleteStudyTask(targetTask.Id);
                CurrentPlanTasks.Remove(targetTask);
                if (SelectedTask == targetTask) SelectedTask = null;
            }
        }

        private void LoadWeeklySchedule()
        {
            WeeklySchedule.Clear();
            
            // Get start of current week (Monday)
            var today = DateTime.Today;
            int daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var monday = today.AddDays(-daysFromMonday);
            
            var schedules = _databaseService.GetScheduleForWeek(monday);
            
            // Create 7 days (always show all days)
            for (int i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                var existing = schedules.FirstOrDefault(s => s.Date.Date == date);
                
                if (existing != null && existing.StudyPlanId > 0)
                {
                    // Get plan name from database
                    var plan = _databaseService.GetStudyPlanById(existing.StudyPlanId);
                    
                    if (plan != null)
                    {
                        existing.PlanName = plan.Subject;
                    }
                    else
                    {
                        // Plan was deleted, show as empty
                        existing.PlanName = "-";
                    }
                    WeeklySchedule.Add(existing);
                }
                else
                {
                    // Add empty placeholder
                    WeeklySchedule.Add(new DailySchedule 
                    { 
                        Date = date, 
                        StudyPlanId = 0, 
                        PlannedMinutes = 0,
                        PlanName = "-"
                    });
                }
            }
        }

        private void CompletePlan(StudyPlan? plan = null)
        {
            var targetPlan = plan ?? SelectedPlan;
            if (targetPlan == null) return;
            
            var result = MessageBox.Show($"'{targetPlan.Subject}' planını tamamlandı olarak işaretlemek istiyor musunuz? (Listeden kaldırılacak)", 
                "Planı Tamamla", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _databaseService.ArchiveStudyPlan(targetPlan.Id);
                Plans.Remove(targetPlan);
                if (SelectedPlan == targetPlan) SelectedPlan = Plans.FirstOrDefault();
                LoadWeeklySchedule();
            }
        }

        private void DeletePlan(StudyPlan? plan = null)
        {
            var targetPlan = plan ?? SelectedPlan;
            if (targetPlan == null) return;
            
            var result = MessageBox.Show($"'{targetPlan.Subject}' planını ve tüm görevlerini silmek istediğinize emin misiniz?", 
                "Planı Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeleteStudyPlan(targetPlan.Id);
                Plans.Remove(targetPlan);
                if (SelectedPlan == targetPlan) SelectedPlan = Plans.FirstOrDefault();
                LoadWeeklySchedule();
            }
        }

        private async Task DistributeToWeek()
        {
            if (SelectedPlan == null) return;
            
            IsGenerating = true;
            try
            {
                var tasks = _databaseService.GetTasksForPlan(SelectedPlan.Id);
                
                if (!tasks.Any())
                {
                    MessageBox.Show("Bu planda görev yok!", "Uyarı");
                    return;
                }
                
                double totalHours = tasks.Sum(t => t.EstimatedHours);
                int studyDays = Math.Min(5, (int)Math.Ceiling(totalHours / 2.0));
                if (studyDays < 1) studyDays = 1;
                double hoursPerDay = totalHours / studyDays;
                
                var today = DateTime.Today;
                int daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var monday = today.AddDays(-daysFromMonday);
                
                var existingSchedules = _databaseService.GetScheduleForWeek(monday);
                foreach (var s in existingSchedules.Where(x => x.StudyPlanId == SelectedPlan.Id))
                {
                    _databaseService.DeleteDailySchedule(s.Id);
                }
                
                for (int i = 0; i < studyDays; i++)
                {
                    var schedule = new DailySchedule
                    {
                        Date = monday.AddDays(i),
                        StudyPlanId = SelectedPlan.Id,
                        PlannedMinutes = (int)(hoursPerDay * 60),
                        IsCompleted = false
                    };
                    _databaseService.AddDailySchedule(schedule);
                }
                
                LoadWeeklySchedule();
                MessageBox.Show($"Plan {studyDays} güne dağıtıldı (günde ~{hoursPerDay:F1} saat)", "Başarılı");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void DeleteSchedule(object? parameter)
        {
            if (parameter is DailySchedule schedule && schedule.Id > 0)
            {
                var result = MessageBox.Show($"{schedule.DayName} günündeki programı silmek istediğinize emin misiniz?", 
                    "Programı Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    _databaseService.DeleteDailySchedule(schedule.Id);
                    LoadWeeklySchedule();
                }
            }
        }

        public void AssignTaskToDay(StudyTask task, DailySchedule day)
        {
            try
            {
                if (SelectedPlan == null)
                {
                    MessageBox.Show("Lütfen önce bir plan seçin!", "Uyarı");
                    return;
                }

                var existingSchedules = _databaseService.GetScheduleForWeek(day.Date.AddDays(-7));
                var existing = existingSchedules.FirstOrDefault(s => 
                    s.Date.Date == day.Date.Date && s.StudyPlanId == SelectedPlan.Id);

                if (existing != null)
                {
                    int additionalMinutes = (int)(task.EstimatedHours * 60);
                    _databaseService.DeleteDailySchedule(existing.Id);
                    
                    var updated = new DailySchedule
                    {
                        Date = day.Date,
                        StudyPlanId = SelectedPlan.Id,
                        PlannedMinutes = existing.PlannedMinutes + additionalMinutes,
                        IsCompleted = false,
                        TaskTopic = task.Topic
                    };
                    _databaseService.AddDailySchedule(updated);
                }
                else
                {
                    var newSchedule = new DailySchedule
                    {
                        Date = day.Date,
                        StudyPlanId = SelectedPlan.Id,
                        PlannedMinutes = (int)(task.EstimatedHours * 60),
                        IsCompleted = false,
                        TaskTopic = task.Topic
                    };
                    _databaseService.AddDailySchedule(newSchedule);
                }

                LoadWeeklySchedule();
                MessageBox.Show($"'{task.Topic}' görevi {day.DayName} gününe atandı!", "Başarılı");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
