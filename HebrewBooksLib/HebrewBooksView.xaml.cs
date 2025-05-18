using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Web.WebView2.Wpf;

namespace HebrewBooksLib
{
    /// <summary>
    /// Interaction logic for HebrewBooksView.xaml
    /// </summary>
    public partial class HebrewBooksView : UserControl
    {
        public HebrewBooksView()
        {
            InitializeComponent();
            //LoadRecentBooks();
            //var vstoDoc = Globals.Factory.GetVstoObject(Globals.ThisAddIn.Application.ActiveDocument);
            //vstoDoc.CloseEvent += () =>  SaveRecentBooks();
        }

        private void SaveRecentBooks()
        {
            var recentBooks = new List<string>();
            foreach (TabItem tabItem in tabControl.Items)
            {
                if (tabItem.Tag is HebrewBooksModel hebrewBooksModel)
                    recentBooks.Add(hebrewBooksModel.ID_Book);
            }

            string json = JsonSerializer.Serialize(recentBooks);
            Interaction.SaveSetting(AppDomain.CurrentDomain.FriendlyName, "HebrewBooks", "Recent", json);
        }

        void LoadRecentBooks()
        {
            try
            {
                string json = Interaction.GetSetting(AppDomain.CurrentDomain.FriendlyName, "HebrewBooks", "Recent");
                if (string.IsNullOrEmpty(json))
                    return;

                var recentBooks = JsonSerializer.Deserialize<List<string>>(json);
                if (recentBooks == null || recentBooks.Count == 0)
                    return;

                foreach (string id in recentBooks)
                {
                    var item = HebrewBooksManager.BookEntries.FirstOrDefault(b => b.ID_Book == id);
                    OpenSelectedBook(item);
                }
            }
            catch { }
        }

        void OpenSelectedBook(HebrewBooksModel entry)
        {
            var webView = new WebView2();
            tabControl.Items.Add(new TabItem { Header = entry.Title, Content = webView, IsSelected = true, Tag = entry });

            LoadBook(webView, entry.ID_Book);
            UpdatePopularity(entry);
        }

        async void UpdatePopularity(HebrewBooksModel entry)
        {
            entry.Popularity++;
            if (entry.Popularity > short.MaxValue)
                await HebrewBooksManager.NormalizePopularityScore();
            await HebrewBooksManager.SaveBookEntriesListAsync();
        }

        async void LoadBook(WebView2 webview, string id)
        {
            try
            {
                webview.CoreWebView2InitializationCompleted += async (s, args) =>
                {
                    if (!args.IsSuccess)
                    {
                        MessageBox.Show("Failed to initialize WebView2.");
                        return;
                    }

                    var loadingAnimation = /*new Uri*/(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "LoadingAnimation.html"));
                    webview.CoreWebView2.Navigate(loadingAnimation);
                    string url = $"https://download.hebrewbooks.org/downloadhandler.ashx?req={id}";
                    string fileName = $"{id}.pdf";
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

                    if (File.Exists(downloadPath))
                        webview.CoreWebView2.Navigate /*new Uri*/(downloadPath);


                };
                await webview.EnsureCoreWebView2Async();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            SaveRecentBooks();

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is HebrewBooksModel hebrewBooksModel)
            {
                OpenSelectedBook(hebrewBooksModel);
                listBox.SelectedIndex = -1;
            }
        }
    }
}
