using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HebrewBooksLib
{
    public static class HebrewBooksManager
    {
        static List<HebrewBooksModel> _bookEntries;
        public static List<HebrewBooksModel> BookEntries
        {
            get { if (_bookEntries == null) { LoadBookEntriesList(); SortList(); } return _bookEntries; }
            set { _bookEntries = value; }
        }

        static string IndexPath
        {
            get => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "HebrewBooks.csv");
        }

        static void LoadBookEntriesList()
        {

            if (File.Exists(IndexPath))
            {
                _bookEntries = new List<HebrewBooksModel>();

                foreach (var entry in File.ReadLines(IndexPath))
                {
                    var splitEntry = entry.Split(',');

                    if (splitEntry.Length == 12)  // Ensure correct number of entries
                    {
                        _bookEntries.Add(new HebrewBooksModel(
                            splitEntry[0], splitEntry[1], splitEntry[2], splitEntry[3],
                            splitEntry[4], splitEntry[5], splitEntry[6], splitEntry[7],
                            splitEntry[8], splitEntry[9], splitEntry[10], int.Parse(splitEntry[11])));
                    }
                }
            }
        }

        public static void SortList()
        {
            _bookEntries = _bookEntries?.OrderByDescending(entry => entry.Popularity)
                  .ThenBy(entry => entry.Title).ToList();
        }

        public static Task SaveBookEntriesListAsync()
        {
            var stb = new StringBuilder();
            foreach (var entry in _bookEntries)
            {
                stb.AppendLine($"{entry.ID_Book},{entry.Title},{entry.Author},{entry.Printing_Place},{entry.Printing_Year},{entry.Pub_Place},{entry.Pub_Date},{entry.Pages},{entry.Catalog_Information},{entry.Content},{entry.Tags},{entry.Popularity.ToString()}");
            }

            File.WriteAllText(IndexPath, stb.ToString());
            return Task.CompletedTask;
        }

        public static List<HebrewBooksModel> Search(string searchTerm)
        {
            var searchTerms = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<HebrewBooksModel> results;

            if (searchTerms.Length == 1 && searchTerms[0].Length < 5)
            {
                results = _bookEntries.Where(entry =>
                entry.Title.StartsWith(searchTerms[0]) ||
                entry.Author.StartsWith(searchTerms[0]) ||
                entry.Tags.StartsWith(searchTerms[0]));
            }
            else
            {
                //var results = BookEntriesList.BookEntries.Where(entry =>
                //   searchTerms.All(term => entry.Title.Contains(term)) || 
                //   searchTerms.All(term => entry.Author.Contains(term)) || 
                //   searchTerms.All(term => entry.Tags.Contains(term))    
                //);

                results = BookEntries.Where(entry => searchTerms.All(
                    term => entry.Title.Contains(term) ||
                    entry.Author.Contains(term) ||
                    entry.Tags.Contains(term)));
            }

            // Combine with recent items, giving priority to frequently accessed ones
            return results
              .OrderByDescending(entry => entry.Popularity)
              .ThenBy(entry => entry.Title).ToList();
        }

        public static Task NormalizePopularityScore()
        {
            Parallel.ForEach(_bookEntries, new ParallelOptions { MaxDegreeOfParallelism = 2 }, entry =>
                entry.Popularity = entry.Popularity / 10);
            return Task.CompletedTask;
        }
    }
}
