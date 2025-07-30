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

            var plcTestViewModel = new PlcTestViewModel(_plcClient, _logger);
  
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _plcService, _logger);

            CurrentPageViewModel = _welcomeViewModel;
            ExitApplicationCommand = new RelayCommand(ExitApplication);
        }

        public void Dispose()
        {
            _plcClient.Dispose();
            _logger.Dispose();
        }
        private void ExitApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Dispose();
                desktop.Shutdown();
            }
        }
    }
}