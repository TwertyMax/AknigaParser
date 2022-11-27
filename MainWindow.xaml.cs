using AudioBooks.Books;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AudioBooks
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateDataGrid()
        {
            BooksGrid.ItemsSource = AudioBooksManager.Books;
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchTextBox.Text != String.Empty)
            {
                await AudioBooksManager.SetBooksBySearch(SearchTextBox.Text);
                if (!AudioBooksManager.Books.Any())
                    AudioBooksManager.Log.AddLogText($"Не удалось найти аудио книгу.");
                else
                    UpdateDataGrid();
                FindedBooksText.Text = $"{AudioBooksManager.Books.Count} аудио книг найдено";
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (AudioBooksManager.IsDownloading)
                return;
            Book book = ((FrameworkElement)sender).DataContext as Book;
            AudioBooksManager.DownloadBook(book);
        }

        private void LogTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LogTextBox.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AudioBooksManager.IsDownloading)
            {
                if (MessageBox.Show(
                    "Загрузка книги продолжается. Вы уверены?",
                    "Предупреждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }
            if (AudioBooksManager.Log.loggingTextCount > 0)
                AudioBooksManager.Log.SaveLog();
        }
    }
}
