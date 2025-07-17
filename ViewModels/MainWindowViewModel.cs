using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel; // Potrebno za ViewModelBase

namespace LM01_UI.ViewModels
{
    // POPRAVEK: Zagotovimo, da razred deduje od ViewModelBase, ki ga verjetno imate
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;

        private readonly WelcomeViewModel _welcomeViewModel;
        private readonly AdminPageViewModel _adminPageViewModel;
        private readonly MainPageViewModel _mainPageViewModel;

        private object? _currentPageViewModel;
        public object? CurrentPageViewModel { get => _currentPageViewModel; set => SetProperty(ref _currentPageViewModel, value); }

        public IRelayCommand ExitApplicationCommand { get; }

        public MainWindowViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _logger = new Logger();
            _plcClient = new PlcTcpClient();

            // Posredujemo iste instance naprej
            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate);
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _logger);

            CurrentPageViewModel = _welcomeViewModel;
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }

        private void Navigate(object target)
        {
            if (target is string pageName)
            {
                // Ustavimo morebitno preverjanje statusa, preden zamenjamo pogled
                if (_currentPageViewModel is MainPageViewModel mvm) mvm.StopPolling();
                if (_currentPageViewModel is AdminPageViewModel avm) { /* TODO: Dodaj StopPolling za Admin, če obstaja */ }

                switch (pageName)
                {
                    case "Run":
                        CurrentPageViewModel = _mainPageViewModel;
                        break;
                    case "Admin":
                        CurrentPageViewModel = _adminPageViewModel;
                        break;
                    case "Welcome":
                        CurrentPageViewModel = _welcomeViewModel;
                        break;
                }
            }
        }
        private void ExitApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _plcClient.Disconnect();
                desktop.Shutdown();
            }
        }
    }
}