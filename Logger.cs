using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;

namespace LM01_UI // POPRAVEK: Pravilen imenski prostor
{
    public class Logger : IDisposable
    {
        private readonly StreamWriter? _logWriter;

        public ObservableCollection<string> Messages { get; } = new();

        private readonly List<string> _pending = new();

        public bool IsFrozen
        {
            get; private set;
        }

        public Logger()
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LM01_UI");
                Directory.CreateDirectory(logDirectory);
                string logFilePath = Path.Combine(logDirectory, "communication_log.txt");
                _logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
                _logWriter.WriteLine($"--- Session started: {DateTime.Now} ---");
            }
            catch (Exception)
            {
                _logWriter = null;
            }
        }

        public void Inform(int type, string message)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss.fff} | {message}";
            try
            {
                _logWriter?.WriteLine(formattedMessage);
            }
            catch (Exception)
            {
                // ignore logging failures
            }

            if (IsFrozen)
            {
                _pending.Add(formattedMessage);
                if (_pending.Count > 200)
                    _pending.RemoveAt(0);
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                Messages.Insert(0, formattedMessage);
                if (Messages.Count > 200)
                    Messages.RemoveAt(Messages.Count - 1);
            });
        }

        public void Dispose()
        {
            if (_logWriter == null)
                return;

            try
            {
                _logWriter.WriteLine($"--- Session ended: {DateTime.Now} ---");
                _logWriter.Flush();
                _logWriter.Dispose();
            }
            catch (Exception)
            {
                // ignore logging failures
            }
        }
        public void ToggleFreeze()
        {
            IsFrozen = !IsFrozen;
            if (!IsFrozen && _pending.Count > 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    for (int i = _pending.Count - 1; i >= 0; i--)
                    {
                        Messages.Insert(0, _pending[i]);
                    }

                    while (Messages.Count > 200)
                        Messages.RemoveAt(Messages.Count - 1);

                    _pending.Clear();
                });
            }
        }

        public void Clear()
        {
            Dispatcher.UIThread.Post(() =>
            {
                Messages.Clear();
                _pending.Clear();
            });
        }
    }
}