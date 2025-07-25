using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LM01_UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly PlcService _plcService;
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
            _plcService = new PlcService();
            _plcClient = new PlcTcpClient(_logger);

            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);

            var plcTestViewModel = new PlcTestViewModel(_plcClient, _logger);
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate, plcTestViewModel);

            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _plcService, _logger);

            CurrentPageViewModel = _welcomeViewModel;
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }

        private void Navigate(object target)
        {
            if (target is string pageName)
            {
                if (_currentPageViewModel is MainPageViewModel mvm) mvm.StopPolling();

                switch (pageName)
                {
                    case "Run":
                        CurrentPageViewModel = _mainPageViewModel;
                        _mainPageViewModel.StartPlcStatusPolling();
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
                Dispose();
                desktop.Shutdown();
            }
        }
    }
}