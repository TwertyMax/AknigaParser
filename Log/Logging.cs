using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace AudioBooks.Log
{
    public enum AddTextType
    {
        AddNew,
        ReplaceLast,
    }

    class Logging
    {
        private List<string> _loggingText = new List<string>();

        public int loggingTextCount => _loggingText.Count;

        public void AddLogText(string text, AddTextType addTextType = AddTextType.AddNew)
        {
            if (addTextType == AddTextType.ReplaceLast)
                _loggingText[_loggingText.Count - 1] = $"{text}";
            else
                _loggingText.Add($"{text}");
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.LogTextBox.Clear();
            foreach (string logText in _loggingText)
            {
                mw.LogTextBox.Text += $"{logText}\n";
            }
        }

        public void SaveLog()
        {
            using(StreamWriter streamWriter = File.CreateText("log.txt"))
                foreach (string logText in _loggingText)
                    streamWriter.WriteLine(logText);
        }
    }
}
