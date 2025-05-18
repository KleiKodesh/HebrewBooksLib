using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WpfLib;

namespace HebrewBooksLib
{
    public class HebrewBooksViewModel : ViewModelBase
    {
        string _searchterm;
        ObservableCollection<HebrewBooksModel> _bookEntries;

        public string Searchterm { get => _searchterm; set { if (SetProperty(ref _searchterm, value)) Search(value); } }
        public ObservableCollection<HebrewBooksModel> BookEntries
        {
            get
            {
                if (_bookEntries == null)
                    _bookEntries = new ObservableCollection<HebrewBooksModel>(HebrewBooksManager.BookEntries);
                return _bookEntries;
            }
            set => SetProperty(ref _bookEntries, value);
        }

        void Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                BookEntries = new ObservableCollection<HebrewBooksModel>(HebrewBooksManager.BookEntries);
                return;
            }

            var searchTerms = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<HebrewBooksModel> results;

            if (searchTerms.Length == 1 && searchTerms[0].Length < 5)
            {
                results = HebrewBooksManager.BookEntries.Where(entry =>
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

                results = HebrewBooksManager.BookEntries.Where(entry => searchTerms.All(
                    term => entry.Title.Contains(term) ||
                    entry.Author.Contains(term) ||
                    entry.Tags.Contains(term)));
            }

            // Combine with recent items, giving priority to frequently accessed ones
            BookEntries = new ObservableCollection<HebrewBooksModel>(results
              .OrderByDescending(entry => entry.Popularity)
              .ThenBy(entry => entry.Title));
        }
    }
}
