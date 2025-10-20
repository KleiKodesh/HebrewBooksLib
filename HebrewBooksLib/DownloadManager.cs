using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using WebViewLib;

namespace HebrewBooksLib
{
    public static class DownloadManager
    {
        static string Section = "SavedFiles";
        static string Key = "defaultFolder";

        static string safeTitle(this string title) => string.Concat(title
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        public async static Task LoadFile(Dispatcher dispatcher, WebView2 webView, HebrewBooksModel hebrewBooksModel)
        {
            string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolder = Path.Combine(myDocumentsPath, "HebrewBooksLib");
            string filePath = Path.Combine(defaultFolder, $"{hebrewBooksModel.Title.safeTitle()}_{hebrewBooksModel.ID_Book.safeTitle()}.pdf");

            if (!File.Exists(filePath))
            {
                var folders = LoadChosenFolders();
                foreach (var folder in folders)
                {
                    filePath = Path.Combine(folder, $"{hebrewBooksModel.Title.safeTitle()}_{hebrewBooksModel.ID_Book.safeTitle()}.pdf");
                    if (File.Exists(filePath)) break;
                }
            }

            if (File.Exists(filePath))
                webView.CoreWebView2.Navigate(filePath);
            else DownloadToTemp(dispatcher, webView, hebrewBooksModel);
        }

        public static void DownloadToTemp(Dispatcher dispatcher, WebView2 webView, HebrewBooksModel entry)
        {
            try
            {
                string url = $"https://download.hebrewbooks.org/downloadhandler.ashx?req={entry.ID_Book}";
                string fileName = $"{entry.ID_Book}.pdf";
                string downloadPath = Path.Combine(Path.GetTempPath(), fileName);

                if (!File.Exists(downloadPath))
                {
                    webView.CoreWebView2.DownloadStarting += (s, e) =>
                    {
                        try
                        {
                            // Set destination file path
                            e.ResultFilePath = downloadPath;

                            // Optional: prevent the default dialog
                            e.Handled = true;

                            var download = e.DownloadOperation;
                            // Hook completed
                            download.StateChanged += (___, ____) =>
                            {
                                if (download.State == CoreWebView2DownloadState.Completed)
                                {
                                    dispatcher.Invoke(() =>
                                    {
                                        // Once downloaded, display the PDF directly
                                        var uri = new Uri(downloadPath).AbsoluteUri;
                                        webView.CoreWebView2.Navigate(uri);
                                    });
                                }
                                else if (download.State == CoreWebView2DownloadState.Interrupted)
                                {
                                    dispatcher.Invoke(() => MessageBox.Show($"Download interrupted: {download.InterruptReason}"));
                                }
                            };
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Download setup error: {ex.Message}");
                        }
                    };

                    // Navigate to file link – Edge handles the download automatically
                    webView.CoreWebView2.Navigate(url);
                }

                webView.NavigationCompleted += (sender, e) =>
                {
                    if (e.IsSuccess)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            while (File.Exists(downloadPath))
                                try { File.Delete(downloadPath); } catch { }
                        });
                    }
                };


                if (File.Exists(downloadPath))
                    webView.CoreWebView2.Navigate(downloadPath);
            }
            catch (Exception fileEx)
            {
                MessageBox.Show("Error saving file: " + fileEx.Message);
            }
        }

        public static async Task CostumeDownloadAsync(HebrewBooksModel hebrewBooksModel)
        {
            // Simple progress dialog while download is happening
            var progressDialog = new ProgressDialog
            {
                WindowTitle = "מוריד קובץ",
                Text = "מוריד את הקובץ...",
                Description = "אנא המתן",
                ShowCancelButton = false,
                ShowTimeRemaining = true
            };
            // Show dialog until download finishes
            bool downloadFinished = false;
            progressDialog.DoWork += (s, e) =>
            {
                while (!downloadFinished)
                    System.Threading.Thread.Sleep(100);
            };

            try
            {
                // Choose folder
                var folderDialog = new VistaFolderBrowserDialog
                {
                    Description = "בחר תיקיה לשמירת הקובץ",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                string defaultFolder = LoadChosenFolders().FirstOrDefault() ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!Directory.Exists(defaultFolder)) Directory.CreateDirectory(defaultFolder);
                folderDialog.SelectedPath = defaultFolder;

                if (folderDialog.ShowDialog() != DialogResult.OK) return;

                string folderPath = folderDialog.SelectedPath;
                SaveChosenFolder(folderPath);

                string fileName = $"{hebrewBooksModel.Title.safeTitle()}_{hebrewBooksModel.ID_Book.safeTitle()}.pdf";
                string destinationPath = Path.Combine(folderPath, fileName);

                progressDialog.Show();

                // Setup WebView2
                var webView = new WebView2();
                webView.Visible = false;
                var env = await CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

                // Optional: intercept downloads
                webView.CoreWebView2.DownloadStarting += (sender, args) =>
                {
                    // Set the download path
                    args.ResultFilePath = destinationPath;

                    // Optionally prevent download UI
                    args.Handled = false;
                };

                string downloadUrl = $"https://download.hebrewbooks.org/downloadhandler.ashx?req={hebrewBooksModel.ID_Book}";

                // Navigate to the download URL (this will trigger the download)
                webView.CoreWebView2.Navigate(downloadUrl);

                webView.CoreWebView2.DownloadStarting += (s, e) =>
                {
                    e.DownloadOperation.BytesReceivedChanged += (sender2, e2) =>
                    {
                        long received = e.DownloadOperation.BytesReceived;
                        long total = (long)e.DownloadOperation.TotalBytesToReceive;
                        if (total > 0)
                            progressDialog.ReportProgress((int)(received * 100 / total));
                    };
                    e.DownloadOperation.StateChanged += (sender3, e3) =>
                    {
                        if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                            downloadFinished = true;
                    };
                };
            }
            catch
            {
                downloadFinished = true;
            }
        }

        static void SaveChosenFolder(string folderPath)
        {
            var folders = LoadChosenFolders();
            if (!folders.Contains(folderPath))
            {
                folders.Add(folderPath);
                string json = JsonSerializer.Serialize(folders);
                Interaction.SaveSetting(AppDomain.CurrentDomain.FriendlyName, Section, Key, json);
            }
        }

        static List<string> LoadChosenFolders()
        {
            try
            {
                string json = Interaction.GetSetting(AppDomain.CurrentDomain.FriendlyName, Section, Key);
                if (!string.IsNullOrEmpty(json))
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { }

            string defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HebrewBooksLib");
            return new List<string> { defaultFolder };
        }

    }
}
