using System;
using System.Windows.Controls;
using StudyMateAI.ViewModels;

namespace StudyMateAI.Views
{
    public partial class VideoPlayerPage : Page
    {
        private VideoPlayerViewModel ViewModel;
        private bool _isWebViewInitialized = false;

        public VideoPlayerPage()
        {
            InitializeComponent();
            ViewModel = new VideoPlayerViewModel();
            DataContext = ViewModel;
            
            // Subscribe to video loaded event
            ViewModel.VideoLoaded += OnVideoLoaded;
            
            // Subscribe to capture request
            ViewModel.CaptureScreenRequested += OnCaptureScreenRequested;
            
            // Initialize WebView2
            InitializeWebView();
        }

        private async System.Threading.Tasks.Task<string> OnCaptureScreenRequested()
        {
            try
            {
                if (!_isWebViewInitialized || VideoWebView.CoreWebView2 == null)
                    return null;

                using (var stream = new System.IO.MemoryStream())
                {
                    await VideoWebView.CoreWebView2.CapturePreviewAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, 
                        stream);
                    
                    var bytes = stream.ToArray();
                    return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture Error: {ex.Message}");
                return null;
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await VideoWebView.EnsureCoreWebView2Async(null);
                _isWebViewInitialized = true;
                
                // Enable DevTools (right-click to open)
                VideoWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                VideoWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                
                // Add console message handler for debugging
                VideoWebView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"WebView Message: {e.WebMessageAsJson}");
                };
                
                // Load a blank page initially
                VideoWebView.CoreWebView2.Navigate("about:blank");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"WebView2 başlatılamadı. Lütfen Microsoft Edge WebView2 Runtime'ı yükleyin.\n\nHata: {ex.Message}", 
                    "WebView2 Hatası", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnVideoLoaded(string videoId)
        {
            if (!_isWebViewInitialized || string.IsNullOrEmpty(videoId))
                return;

            try
            {
                // Instead of using iframe API (which has embedding restrictions),
                // we'll load YouTube directly as a web page
                string youtubeUrl = $"https://www.youtube.com/watch?v={videoId}";
                
                // Navigate directly to YouTube
                VideoWebView.CoreWebView2.Navigate(youtubeUrl);
                
                // Optional: Inject CSS to hide YouTube's header/sidebar for cleaner look
                VideoWebView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (e.IsSuccess)
                    {
                        // Inject custom CSS to make it look more embedded
                        string script = @"
                            (function() {
                                // Hide YouTube header, sidebar, and recommendations for cleaner look
                                var style = document.createElement('style');
                                style.textContent = `
                                    /* Hide masthead (top bar) */
                                    #masthead-container { display: none !important; }
                                    ytd-masthead { display: none !important; }
                                    
                                    /* Hide sidebar */
                                    #related { display: none !important; }
                                    #secondary { display: none !important; }
                                    
                                    /* Make video player take full width */
                                    #primary { max-width: 100% !important; margin: 0 !important; }
                                    #player-container { width: 100% !important; }
                                    
                                    /* Hide comments and other distractions */
                                    #comments { display: none !important; }
                                    ytd-watch-metadata { display: none !important; }
                                    
                                    /* Center the player */
                                    body { background: #000 !important; }
                                    ytd-app { background: #000 !important; }
                                    
                                    /* Make player responsive */
                                    .html5-video-player { width: 100% !important; height: 100vh !important; }
                                `;
                                document.head.appendChild(style);
                            })();
                        ";
                        
                        VideoWebView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Video yüklenirken hata: {ex.Message}", "Hata");
            }
        }
    }
}
