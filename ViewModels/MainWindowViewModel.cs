using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;

namespace LM01_UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        // IZBRISANO: Statičnega razreda ne moremo imeti kot polje (field).
        // private readonly PlcService _plcService; 
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
            // IZBRISANO: Statičnega razreda ne moremo ustvariti z 'new'.
            // _plcService = new PlcService(); 

            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);
            // POPRAVEK: AdminPageViewModel verjetno tudi ne potrebuje _plcService, če ga nismo popravljali.
            // Predpostavljam, da ga bomo kasneje, zaenkrat pustim nespremenjeno.
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate);

            // POPRAVEK: Odstranjen _plcService iz klicev konstruktorja.
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _logger);

            CurrentPageViewModel = _welcomeViewModel;
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }

        private void Navigate(object target)
        {
            if (target is string pageName)
            {
                switch (pageName)
                {
                    case "Run":
                        _mainPageViewModel.LoadRecipesCommand.Execute(null);
                        CurrentPageViewModel = _mainPageViewModel;
                        break;
                    case "Admin":
                        // Stop polling on the main page before switching.
                        _mainPageViewModel.StopPolling();
                        CurrentPageViewModel = _adminPageViewModel;
                        break;
                    case "Welcome":
                        // Stop polling on the main page before switching.
                        _mainPageViewModel.StopPolling();
                        CurrentPageViewModel = _welcomeViewModel;
                        break;
                }
            }
        }
        private void ExitApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _plcClient.Disconnect(); // Pospravimo za seboj
                desktop.Shutdown();
            }
        }
    }
}