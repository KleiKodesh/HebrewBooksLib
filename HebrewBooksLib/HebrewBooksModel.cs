using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HebrewBooksLib
{
    public class HebrewBooksModel
    {
        public string ID_Book { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Printing_Place { get; set; }
        public string Printing_Year { get; set; }
        public string Pub_Place { get; set; }
        public string Pub_Date { get; set; }
        public string Pages { get; set; }
        public string Catalog_Information { get; set; }
        public string Content { get; set; }
        public string Tags { get; set; }
        public double Popularity { get; set; }

        public HebrewBooksModel(string iD_Book, string title, string author, string printing_Place, string printing_Year, string pub_Place, string pub_Date, string pages, string catalog_Information, string content, string tags, int popularity)
        {
            ID_Book = iD_Book;
            Title = title;
            Author = author;
            Printing_Year = printing_Year;
            Pub_Place = pub_Place;
            Pub_Date = pub_Date;
            Pages = pages;
            Catalog_Information = catalog_Information;
            Content = content;
            Popularity = popularity;

            Tags = tags.Replace(";", " \\ ");
            if (!string.IsNullOrEmpty(printing_Place))
            {
                if (!printing_Place.Contains(printing_Year))
                    Printing_Place = $"(דפוס {printing_Place} - {printing_Year})";
                else
                    Printing_Place = printing_Place;
            }
        }
    }
}
