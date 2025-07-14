using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using LM01_UI.Data.Persistence;
using LM01_UI.ViewModels;
using LM01_UI.Views;
using Microsoft.EntityFrameworkCore;
// NOVO: Dodane potrebne using direktive
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace LM01_UI
{
    public partial class App : Application
    {
        // NOVO: Statična lastnost za dostop do konfiguracije iz celotne aplikacije
        public static IConfiguration Configuration { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // NOVO: Koda za nalaganje konfiguracije iz appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            // NOVO: Ustvarjanje DbContexta z uporabo connection stringa iz konfiguracije
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connectionString);

            // Ustvarimo eno instanco DbContexta, ki jo bomo posredovali naprej
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            dbContext.Database.Migrate(); // Zagotovi, da je baza posodobljena.


            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();

                desktop.MainWindow = new MainWindow
                {
                    // NOVO: V MainWindowViewModel posredujemo DbContext
                    DataContext = new MainWindowViewModel(dbContext),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}