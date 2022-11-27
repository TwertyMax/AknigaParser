using System;

namespace AudioBooks.Books
{
    class Book
    {
        private int _hoursDuration;
        private int _minutesDuration;

        public int BookID { get; private set; }

        public string Author { get; private set; }
        public string Name { get; private set; }
        public string ReaderName { get; private set; }
        public string BookSeries { get; private set; }
        public string Duration { get { return String.Format("{0} {1} {2} {3}", _hoursDuration, _hoursDuration != 1 ? "часов" : "час", _minutesDuration, _minutesDuration != 1 ? "минут" : "минута"); } }
        public string BookURL { get; private set; }

        public Book(int bookID, string author, string name, string readerName, string bookSeries, int hoursDuration, int minutesDuration, string url)
        {
            BookID = bookID;
            Author = author;
            Name = name;
            ReaderName = readerName;
            BookSeries = bookSeries;
            _hoursDuration = hoursDuration;
            _minutesDuration = minutesDuration;
            BookURL = url;
        }
    }
}
