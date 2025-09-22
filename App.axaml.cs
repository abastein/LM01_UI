using Avalonia;
using Avalonia.Controls; // Potrebno za Window
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LM01_UI.Data.Persistence;
using LM01_UI.ViewModels;
using LM01_UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace LM01_UI
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString);

            var dbContext = new ApplicationDbContext(optionsBuilder.Options);
            dbContext.EnsureSystemRecipesAsync().GetAwaiter().GetResult();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //var mainViewModel = new MainWindowViewModel(dbContext);
                //desktop.MainWindow = new MainWindow
                //{
                //    DataContext = mainViewModel
                //};
                var mainViewModel = new MainWindowViewModel(dbContext);
                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel,
                    WindowState = WindowState.FullScreen,
                    SystemDecorations = SystemDecorations.None
                };

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        // POPRAVEK: Tip povratne vrednosti je sedaj Window? (nullable)
        public Window? GetMainWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            // V primeru, da okno ne obstaja, vrnemo null, namesto da sprožimo izjemo
            return null;
        }
    }
}