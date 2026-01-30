using System.Windows;
using System.Windows.Controls;
using StudyMateAI.Services;
using StudyMateAI.Views;

namespace StudyMateAI
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly OllamaService _ollamaService;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Initialize services
                _databaseService = new DatabaseService();
                _ollamaService = new OllamaService();
                
                // Enable window dragging
                this.MouseLeftButtonDown += (s, e) => DragMove();
                
                // Load default page after UI is ready
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CRITICAL ERROR in MainWindow constructor:\n\n{ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainFrame != null)
                {
                    MainFrame.Navigate(new DashboardPage(_databaseService, _ollamaService));
                }
                else
                {
                    MessageBox.Show("MainFrame is null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignore if MainFrame is not ready yet
            if (MainFrame == null) return;
            
            var selectedIndex = MenuListBox.SelectedIndex;
            
            switch (selectedIndex)
            {
                case 0: // Dashboard
                    MainFrame.Navigate(new DashboardPage(_databaseService, _ollamaService));
                    PageTitle.Text = "Dashboard";
                    break;
                case 1: // Derslerim
                    MainFrame.Navigate(new CoursesPage(_databaseService));
                    PageTitle.Text = "Derslerim";
                    break;
                case 2: // AI Asistan
                    MainFrame.Navigate(new ChatPage());
                    PageTitle.Text = "AI Asistan";
                    break;
                case 3: // Dökümanlar
                    MainFrame.Navigate(new DocumentsPage());
                    PageTitle.Text = "Dökümanlar";
                    break;
                case 4: // Planlayıcı
                    MainFrame.Navigate(new StudyPlannerPage());
                    PageTitle.Text = "Çalışma Planlayıcı";
                    break;
                case 5: // Video Oynatıcı
                    MainFrame.Navigate(new VideoPlayerPage());
                    PageTitle.Text = "Video Oynatıcı";
                    break;
                case 6: // Sınav Simülasyonu
                    MainFrame.Navigate(new ExamPage());
                    PageTitle.Text = "Sınav Simülasyonu";
                    break;
                case 7: // Ayarlar
                    MainFrame.Navigate(new SettingsPage());
                    PageTitle.Text = "Ayarlar";
                    break;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}