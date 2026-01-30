using System.Windows.Controls;

namespace StudyMateAI.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            var tempService = new StudyMateAI.Services.OllamaService(); 
            DocumentModelTextBox.Text = StudyMateAI.Services.OllamaService.DocumentModel;
            ChatModelTextBox.Text = StudyMateAI.Services.OllamaService.ChatModel;
            VideoModelTextBox.Text = StudyMateAI.Services.OllamaService.VideoModel;
            GeneralModelTextBox.Text = StudyMateAI.Services.OllamaService.GeneralModel;
        }

        private async void SaveModel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string docModel = DocumentModelTextBox.Text.Trim();
            string chatModel = ChatModelTextBox.Text.Trim();
            string videoModel = VideoModelTextBox.Text.Trim();
            string genModel = GeneralModelTextBox.Text.Trim();

            if (string.IsNullOrEmpty(docModel) || string.IsNullOrEmpty(chatModel) || string.IsNullOrEmpty(videoModel) || string.IsNullOrEmpty(genModel))
            {
                System.Windows.MessageBox.Show("Lütfen tüm model alanlarını doldurun.", "Uyarı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var service = new StudyMateAI.Services.OllamaService();

            // Check connection first
            if (!await service.IsOllamaRunningAsync())
            {
                System.Windows.MessageBox.Show("Ollama servisine ulaşılamıyor. Lütfen Ollama'nın terminalde çalıştığından emin olun (http://localhost:11434).", 
                    "Bağlantı Hatası", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            bool allValid = true;

            // Helper function to verify model and update icon
            async System.Threading.Tasks.Task VerifyAndUpdateIcon(string model, MaterialDesignThemes.Wpf.PackIcon icon)
            {
                bool exists = await service.CheckModelExistsAsync(model);
                icon.Kind = exists ? MaterialDesignThemes.Wpf.PackIconKind.CheckCircle : MaterialDesignThemes.Wpf.PackIconKind.CloseCircle;
                icon.Foreground = exists ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                icon.Visibility = System.Windows.Visibility.Visible;
                if (!exists) allValid = false;
            }

            // Disable button while verifying
            SaveButton.IsEnabled = false;

            await VerifyAndUpdateIcon(docModel, DocumentModelStatusIcon);
            await VerifyAndUpdateIcon(chatModel, ChatModelStatusIcon);
            await VerifyAndUpdateIcon(videoModel, VideoModelStatusIcon);
            await VerifyAndUpdateIcon(genModel, GeneralModelStatusIcon);

            SaveButton.IsEnabled = true;

            if (allValid)
            {
                StudyMateAI.Services.OllamaService.SetModels(docModel, chatModel, videoModel, genModel);
                System.Windows.MessageBox.Show("Tüm AI modelleri doğrulandı ve güncellendi.", "Başarılı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                var result = System.Windows.MessageBox.Show("Bazı modeller Ollama'da bulunamadı (Kırmızı işaretliler). Yine de kaydetmek ister misiniz?", "Model Doğrulama Hatası", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    StudyMateAI.Services.OllamaService.SetModels(docModel, chatModel, videoModel, genModel);
                }
            }
        }
    }
}
