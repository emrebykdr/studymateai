using System.Windows;

namespace StudyMateAI.Views
{
    public partial class FolderCreationDialog : Window
    {
        public string FolderName { get; private set; } = string.Empty;

        public FolderCreationDialog()
        {
            InitializeComponent();
            FolderNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderNameTextBox.Text))
            {
                MessageBox.Show("Klasör adı boş olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FolderName = FolderNameTextBox.Text.Trim();
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
