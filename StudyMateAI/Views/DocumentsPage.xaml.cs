using System;
using System.Windows;
using System.Windows.Controls;
using StudyMateAI.Models;
using StudyMateAI.ViewModels;

namespace StudyMateAI.Views
{
    public partial class DocumentsPage : Page
    {
        public DocumentsPage()
        {
                InitializeComponent();
            DataContext = new DocumentViewModel();
            
            // WebView2 arka plan rengini ayarla
            Loaded += DocumentsPage_Loaded;
        }

        private async void DocumentsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // WebView2'nin hazır olmasını bekle
                await PdfViewer.EnsureCoreWebView2Async(null);
                // Koyu gri arka plan rengi ayarla
                PdfViewer.DefaultBackgroundColor = System.Drawing.Color.FromArgb(30, 41, 59); // #1E293B
            }
            catch
            {
                // WebView2 henüz hazır değilse sessizce geç
            }
        }


    }
}
