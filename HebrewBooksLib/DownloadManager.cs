using Microsoft.VisualBasic;
using WebViewLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HebrewBooksLib
{
    public static class DownloadManager
    {
        static string Section = "SavedFiles";
        static string Key = "defaultFolder";

        static string safeTitle(this string title) => string.Concat(title
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        public static void LoadFile(WebViewHost webView, HebrewBooksModel hebrewBooksModel)
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
                webView.Navigate(filePath);
            else DownloadToTemp(webView, hebrewBooksModel);
        }

        public async static void DownloadToTemp(WebViewHost webView, HebrewBooksModel entry)
        {

            try
            {
                string url = $"https://download.hebrewbooks.org/downloadhandler.ashx?req={entry.ID_Book}";
                string fileName = $"{entry.ID_Book}.pdf";
                string downloadPath = Path.Combine(Path.GetTempPath(), fileName);

                if (!File.Exists(downloadPath))
                {
                    var handler = new HttpClientHandler { UseCookies = true };
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 ...");
                        client.DefaultRequestHeaders.Add("Referer", "https://www.hebrewbooks.org/");

                        byte[] fileBytes = await client.GetByteArrayAsync(url);

                        File.WriteAllBytes(downloadPath, fileBytes);
                    }
                }

                webView.WebView.NavigationCompleted += (sender, e) =>
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
                    webView.Navigate(downloadPath);
            }
            catch (Exception fileEx)
            {
                MessageBox.Show("Error saving file: " + fileEx.Message);
            }
        }

        public static void Download(HebrewBooksModel hebrewBooksModel)
        {
            var folderDialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog
            {
                Description = "בחר תיקיה לשמירת הקובץ",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            string defaultFolder = LoadChosenFolders().FirstOrDefault();
            if (!Directory.Exists(defaultFolder)) Directory.CreateDirectory(defaultFolder);

            folderDialog.SelectedPath = defaultFolder;

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = folderDialog.SelectedPath;
                SaveChosenFolder(folderPath);

                string fileName = $"{hebrewBooksModel.Title.safeTitle()}_{hebrewBooksModel.ID_Book.safeTitle()}.pdf";
                string destinationPath = Path.Combine(folderPath, fileName);
                string downloadUrl = $"https://download.hebrewbooks.org/downloadhandler.ashx?req={hebrewBooksModel.ID_Book}";

                var progressDialog = new Ookii.Dialogs.WinForms.ProgressDialog
                {
                    WindowTitle = "מוריד קובץ",
                    Text = "מוריד את הקובץ...",
                    Description = "אנא המתן",
                    ShowCancelButton = false,
                    ShowTimeRemaining = true
                };

                progressDialog.DoWork += (sender2, e2) =>
                {
                    var dialog = (Ookii.Dialogs.WinForms.ProgressDialog)sender2;

                    var handler = new HttpClientHandler { UseCookies = true };
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 ...");
                        client.DefaultRequestHeaders.Add("Referer", "https://www.hebrewbooks.org/");

                        using (var response = client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).Result)
                        {
                            response.EnsureSuccessStatusCode();

                            long? totalBytes = response.Content.Headers.ContentLength;
                            using (var stream = response.Content.ReadAsStreamAsync().Result)
                            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                byte[] buffer = new byte[8192];
                                long totalRead = 0;
                                int bytesRead;

                                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                    totalRead += bytesRead;

                                    if (totalBytes.HasValue)
                                    {
                                        int progress = (int)((totalRead * 100) / totalBytes.Value);
                                        dialog.ReportProgress(progress);
                                    }
                                }
                            }
                        }
                    }
                };

                progressDialog.Show();
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
