using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudyMateAI.Models;
using StudyMateAI.ViewModels;

namespace StudyMateAI.Views
{
    public partial class StudyPlannerPage : Page
    {
        private StudyPlannerViewModel? ViewModel => DataContext as StudyPlannerViewModel;

        public StudyPlannerPage()
        {
            InitializeComponent();
            DataContext = new StudyPlannerViewModel();
        }

        private void TaskItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is StudyTask task)
            {
                // Start drag operation
                DragDrop.DoDragDrop(grid, task, DragDropEffects.Copy);
            }
        }

        private void DayItem_DragOver(object sender, DragEventArgs e)
        {
            // Check if we're dragging a StudyTask
            if (e.Data.GetDataPresent(typeof(StudyTask)))
            {
                e.Effects = DragDropEffects.Copy;
                
                // Visual feedback - highlight the drop target
                if (sender is Border border)
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(30, 100, 150, 255));
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DayItem_Drop(object sender, DragEventArgs e)
        {
            // Reset background
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
                
                if (e.Data.GetDataPresent(typeof(StudyTask)) && border.DataContext is DailySchedule day)
                {
                    var task = e.Data.GetData(typeof(StudyTask)) as StudyTask;
                    if (task != null && ViewModel != null)
                    {
                        ViewModel.AssignTaskToDay(task, day);
                    }
                }
            }
            e.Handled = true;
        }
    }
}
