using System;
using Avalonia.Threading;

namespace LM01_UI
{
    public class Logger
    {
        public Logger()
        {
            // Konstruktor brez parametra
        }

        public void Inform(short kind, string msg, Action<string>? logAction = null)
        {
            string prefix = $"{DateTime.Now:HH:mm:ss} | ";
            string line = $"{prefix}{msg}";

            if (logAction != null)
            {
                logAction.Invoke(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }
    }
}
