using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using AudioBooks.Extensions;

namespace AudioBooks.Books
{
    class AudioBooksParser
    {

        public async Task<List<Book>> SearchAudioBooks(string searchRequest)
        {
            List<Book> books = new List<Book>();
            HtmlWeb htmlWeb = new HtmlWeb();
            try
            {
                AudioBooksManager.Log.AddLogText("Начат поиск книг.");
                HtmlDocument htmlDocument = await htmlWeb.LoadFromWebAsync($@"https://akniga.org/search/books?q={searchRequest}");
                int pageCount = GetSearchPagesCount(htmlDocument);
                await GetAllPages(htmlWeb, books, searchRequest, pageCount);
                AudioBooksManager.Log.AddLogText($"Поиск завершен. Всего было найдено {books.Count} книг на {pageCount} страницах.");
                return books;
            }
            catch (Exception ex)
            {
                AudioBooksManager.Log.AddLogText($"Не удалось найти аудио книгу. Причина: {ex.Message}.");
            }
            return books;
        }

        public void CheckCopyright(IWebDriver driver)
        {
            if (driver.IsElementPresent(By.XPath("//div[contains(@class, 'book--item--closed-text')]")))
                throw new Exception("Аудио книга недоступна из-за АП");
        }

        public int GetFilesCount(int bookID, string url)
        {
            Regex regexFilesCount = new Regex($".*?://.*?/b/{bookID}/.*?/(..).*", RegexOptions.Compiled);
            Match regexMatchFilesCount = regexFilesCount.Match(url);
            return Int32.Parse(regexMatchFilesCount.Groups[1].Value);
        }

        public string GetBookURLDownload(int bookID, IWebDriver driver)
        {
            driver.FindElement(By.XPath($"//div[contains(@class, 'bookpage--chapters player--chapters') and @data-bid='{bookID}']")).FindElements(By.XPath("./div[contains(@class, 'chapter__default')]")).Last().Click();
            return driver.FindElement(By.XPath($"//div[contains(@class, 'jpl') and @data-bid='{bookID}']")).FindElement(By.XPath("./audio")).GetAttribute("src"); ;
        }

        public async Task GetAllPages(HtmlWeb htmlWeb, List<Book> books, string searchRequest, int pageCount)
        {
            for (int i = 0; i < pageCount; i++)
                await GetAllBooksFromPage(htmlWeb, books, $"https://akniga.org/search/books/page{i + 1}/?q={searchRequest}");
        }

        public async Task GetAllBooksFromPage(HtmlWeb htmlWeb, List<Book> books, string pageURL)
        {
            HtmlDocument htmlDocument = await htmlWeb.LoadFromWebAsync(pageURL);
            var htmlBooks = htmlDocument.DocumentNode.SelectNodes("//div[@data-bid]");
            if (htmlBooks != null)
            {
                foreach (var htmlBook in htmlBooks)
                {
                    try
                    {
                        int bookID = GetBookID(htmlBook);
                        (int bookDurationHours, int bookDurationMinutes) = GetBookDuration(htmlBook);
                        (string bookAuthor, string bookName) = GetBookAuthorName(htmlBook);
                        string bookReaderName = GetBookReaderName(htmlBook);
                        string bookSeries = GetBookSeries(htmlBook);
                        string bookURL = GetBookURL(htmlBook);
                        books.Add(new Book(bookID, bookAuthor, bookName, bookReaderName, bookSeries, bookDurationHours, bookDurationMinutes, bookURL));
                    }
                    catch (Exception ex)
                    {
                        AudioBooksManager.Log.AddLogText($"Не удалось найти аудио книгу. Причина: {ex.Message}.");
                    }
                }
            }
        }

        public int GetSearchPagesCount(HtmlDocument searchPage)
        {
            if (searchPage.DocumentNode.SelectSingleNode("//div[contains(@class, 'page__nav')]") != null)
                return Int32.Parse(searchPage.DocumentNode.SelectNodes("//a[contains(@class, 'page__nav--standart')]").Last().InnerText);
            return 1;
        }

        public int GetBookID(HtmlNode bookClass) => Int32.Parse(bookClass.Attributes["data-bid"].Value);

        public (int, int) GetBookDuration(HtmlNode bookClass)
        {
            int hours = 0;
            int minutes = 0;
            string hoursText = bookClass.SelectSingleNode(".//span[contains(@class, 'hours')]").InnerText;
            string minutesText = bookClass.SelectSingleNode(".//span[contains(@class, 'minutes')]").InnerText;
            Regex regexBookDuration = new Regex(@"(\d+)", RegexOptions.Compiled);
            Match hoursDurationMatch = regexBookDuration.Match(hoursText);
            Match minutesDurationMatch = regexBookDuration.Match(minutesText);
            if (hoursDurationMatch.Success)
                hours = Int32.Parse(hoursDurationMatch.Groups[1].Value);
            if (minutesDurationMatch.Success)
                minutes = Int32.Parse(minutesDurationMatch.Groups[1].Value);
            return (hours, minutes);
        }

        public string GetBookAuthor(HtmlNode bookClass)
        {
            string author = bookClass.SelectSingleNode(".//span[contains(@class, 'link__action link__action--author')]")?.SelectSingleNode(".//a").InnerText;
            if (!String.IsNullOrEmpty(author))
                return author;
            return "-";
        }

        public string GetBookName(HtmlNode bookClass)
        {
            string name = bookClass.SelectSingleNode(".//h2[contains(@class, 'caption__article-main')]").InnerText.Replace("\n", String.Empty).Trim();
            if (!String.IsNullOrEmpty(name))
                return name;
            return "-";
        }

        public (string, string) GetBookAuthorName(HtmlNode bookClass)
        {
            Regex regexBookFullName = new Regex("^(?<Author>.*?) - (?<Name>.*)", RegexOptions.Compiled);
            Match fullNameMatch = regexBookFullName.Match(bookClass.SelectSingleNode(".//h2[contains(@class, 'caption__article-main')]").InnerText.Replace("\n", String.Empty).Trim());
            if (!fullNameMatch.Success)
                return (GetBookAuthor(bookClass), GetBookName(bookClass));
            return (fullNameMatch.Groups["Author"].Value, fullNameMatch.Groups["Name"].Value);
        }

        public string GetBookReaderName(HtmlNode bookClass)
        {
            var readerName = bookClass.SelectNodes(".//span[contains(@class, 'link__action link__action--author')]");
            if (readerName != null && readerName.Count > 1)
                return readerName[1].SelectSingleNode(@".//a").InnerText;
            return "-";
        }

        public string GetBookSeries(HtmlNode bookClass)
        {
            var readerName = bookClass.SelectNodes(".//span[contains(@class, 'link__action link__action--author')]");
            if (readerName != null && readerName.Count > 2)
                return readerName[2].SelectSingleNode(".//a").InnerText;
            return "-";
        }

        public int GetBookPageBookID(IWebDriver driver) => Int32.Parse(driver.FindElement(By.XPath($"//div[@data-bid]")).GetAttribute("data-bid"));

        public string GetBookURL(HtmlNode bookClass) => bookClass.SelectSingleNode(".//a[contains(@class, 'content__article-main-link tap-link')]").Attributes["href"].Value;
    }
}

