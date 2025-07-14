using Avalonia;
using System;
using System.Globalization; // DODAJTE TA USING
using System.Threading;     // DODAJTE TA USING

namespace LM01_UI
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // DODAJTE TE DVE VRSTICI NA SAM ZAČETEK METODE
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sl-SI");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("sl-SI");

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}