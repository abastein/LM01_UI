using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;

namespace LM01_UI.Services
{
    public class Logger
    {
        /// <summary>
        /// A collection of all log messages that the UI can bind to.
        /// </summary>
        public ObservableCollection<string> Messages { get; } = new();

        /// <summary>
        /// Adds a new formatted message to the collection.
        /// </summary>
        public void Inform(int type, string message)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss} | {message}";

            // Use the dispatcher to ensure the collection is always modified on the UI thread,
            // which is required for UI binding to work safely.
            Dispatcher.UIThread.Invoke(() =>
            {
                Messages.Insert(0, formattedMessage);
            });
        }
    }
}
