using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using AudioBooks.Log;
using System.Net.Http;

namespace AudioBooks.Books
{
    static class AudioBooksManager
    {
        public static AudioBooksParser Parser { get; private set; }

        public static BindingList<Book> Books { get; private set; }

        public static Logging Log { get; private set; }

        private static readonly HttpClient _httpClient;

        public static bool IsDownloading;

        static AudioBooksManager()
        {
            Log = new Logging();

            Parser = new AudioBooksParser();

            _httpClient = new HttpClient();
            _httpClient.Timeout = new TimeSpan(-00, 00, 00, 0010000);
        }

        public static string GetBookPath(Book book) => $@"{Environment.CurrentDirectory}\{book.Author} - {book.Name}";

        public static async Task SetBooksBySearch(string searchRequest)
        {
            Books = new BindingList<Book>(await Parser.SearchAudioBooks(searchRequest));
        }

        public static (ChromeOptions, ChromeDriverService) SetChromeOptions()
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();

            chromeOptions.AddArguments("headless");
            chromeDriverService.HideCommandPromptWindow = true;

            return (chromeOptions, chromeDriverService);
        }

        public static (string, int) GetBookData(Book book)
        {
            (ChromeOptions chromeOptions, ChromeDriverService chromeDriverService) = SetChromeOptions();
            using (IWebDriver driver = new ChromeDriver(chromeDriverService, chromeOptions))
            {
                driver.Navigate().GoToUrl(book.BookURL);
                Parser.CheckCopyright(driver);
                string urlToDownload = Parser.GetBookURLDownload(book.BookID, driver);
                int fileCount = Parser.GetFilesCount(book.BookID, urlToDownload);
                return (urlToDownload, fileCount);
            }
        }

        public static async void DownloadBook(Book book)
        {
            try
            {
                (string urlToDownload, int fileCount) = GetBookData(book);
                await DownloadAudioFiles(book, urlToDownload, fileCount);
            }
            catch (Exception ex)
            {
                AudioBooksManager.IsDownloading = false;
                Log.AddLogText($"Не удалось скачать аудио книгу. Причина: {ex.Message}.");
            }
        }

        private static async Task DownloadAudioFiles(Book book, string urlToDownload, int fileCount)
        {
            Log.AddLogText($"Загрузка книги: {book.Author} - {book.Name} из серии {book.BookSeries} началась.");
            if (!Directory.Exists(GetBookPath(book)))
                Directory.CreateDirectory(GetBookPath(book));
            for (int i = 0; i < fileCount; i++)
            {
                string link = urlToDownload.Replace(fileCount < 10 ? $"0{fileCount}" : $"{fileCount}", $"0{i + 1}");
                AudioBooksManager.IsDownloading = true;
                Log.AddLogText($"Файл {i}.mp3 по ссылке {link} загружается в {GetBookPath(book)}.");
                var response = await _httpClient.GetAsync(link);
                int size = int.Parse(response.Content.Headers.First(h => h.Key.Equals("Content-Length")).Value.First());
                using (FileStream fileStream = File.Create(GetBookPath(book) + $@"\{i}.mp3"))
                {
                    Log.AddLogText($"Файл был загружен усешно. Размер файла - {size / 1000000} MB.");
                    await response.Content.CopyToAsync(fileStream);
                }
            }
            AudioBooksManager.IsDownloading = false;
            Log.AddLogText($"Загрузка книги: {book.Author} - {book.Name} из серии {book.BookSeries} завершена успешна.");
        }
    }
}
