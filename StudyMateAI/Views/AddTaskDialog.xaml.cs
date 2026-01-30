using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace StudyMateAI.Views
{
    public partial class AddTaskDialog : Window
    {
        public string Topic { get; private set; }
        public double Hours { get; private set; }

        public AddTaskDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TopicTextBox.Text))
            {
                MessageBox.Show("Lütfen bir konu adı giriniz.", "Uyarı");
                return;
            }

            Topic = TopicTextBox.Text;
            if (double.TryParse(HoursTextBox.Text, out double h))
            {
                Hours = h;
            }
            else
            {
                Hours = 1.0;
            }

            DialogResult = true;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
