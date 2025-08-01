using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace LM01_UI // POPRAVEK: Pravilen imenski prostor
{
    public class Logger : IDisposable
    {
       // private readonly StreamWriter _logWriter;

        public ObservableCollection<string> Messages { get; } = new();

        public Logger()
        {
           // string logFilePath = Path.Combine(AppContext.BaseDirectory, "communication_log.txt");
          //  _logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
          //  _logWriter.WriteLine($"--- Nova Seja Začeta: {DateTime.Now} ---");
        }

        public void Inform(int type, string message)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss.fff} | {message}";
          //  _logWriter.WriteLine(formattedMessage);

            Dispatcher.UIThread.Post(() =>
            {
                Messages.Insert(0, formattedMessage);
                if (Messages.Count > 200)

                    Messages.RemoveAt(Messages.Count - 1);

            });
        }

        public void Dispose()
        {
           // _logWriter.WriteLine($"--- Seja Končana: {DateTime.Now} ---");
           // _logWriter.Dispose();
        }
    }
}