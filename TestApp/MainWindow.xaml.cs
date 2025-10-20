using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace TestApp
{
    public partial class MainWindow : Window
    {
        private const string RemotePdfUrl = "https://download.hebrewbooks.org/downloadhandler.ashx?req=67022";
        private string _tempPdfPath;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Browser.EnsureCoreWebView2Async();

            _tempPdfPath = Path.Combine(Path.GetTempPath(), "downloaded_book.pdf");

            // Subscribe before navigation
            Browser.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

            // Navigate to file link – Edge handles the download automatically
            Browser.CoreWebView2.Navigate(RemotePdfUrl);
        }

        private void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            try
            {
                // Set destination file path
                e.ResultFilePath = _tempPdfPath;

                // Optional: prevent the default dialog
                e.Handled = true;

                var download = e.DownloadOperation;

                // Hook progress changed
                download.BytesReceivedChanged += (_, __) =>
                {
                    double total = download.TotalBytesToReceive ?? 0; // handle null
                    double percent = total > 0
                        ? (double)download.BytesReceived / total * 100
                        : 0;

                    Dispatcher.Invoke(() => Title = $"Downloading... {percent:F1}%");
                };


                // Hook completed
                download.StateChanged += (_, __) =>
                {
                    if (download.State == CoreWebView2DownloadState.Completed)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Title = "Download complete!";
                            // Once downloaded, display the PDF directly
                            var uri = new Uri(_tempPdfPath).AbsoluteUri;
                            Browser.CoreWebView2.Navigate(uri);
                        });
                    }
                    else if (download.State == CoreWebView2DownloadState.Interrupted)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Download interrupted: {download.InterruptReason}"));
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download setup error: {ex.Message}");
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_tempPdfPath) && File.Exists(_tempPdfPath))
                    File.Delete(_tempPdfPath);
            }
            catch { /* ignore cleanup errors */ }
        }
    }
}
