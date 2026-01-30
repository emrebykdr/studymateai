using System.Windows.Controls;
using StudyMateAI.ViewModels;
using System.Collections.Specialized;

namespace StudyMateAI.Views
{
    public partial class ChatPage : Page
    {
        private readonly ChatViewModel _viewModel;

        public ChatPage()
        {
            InitializeComponent();
            _viewModel = new ChatViewModel();
            DataContext = _viewModel;

            // Auto-scroll to bottom when new messages arrive
            _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                MessageScrollViewer.ScrollToEnd();
            }
        }
    }
}
