using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace LM01_UI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly PlcService _plcService;
        private readonly Logger _logger;
        private readonly WelcomeViewModel _welcomeViewModel;
        private readonly AdminPageViewModel _adminPageViewModel;
        private readonly MainPageViewModel _mainPageViewModel;

        [ObservableProperty]
        private string _plcStatusText = "PLC ni povezan";

        [ObservableProperty]
        private bool _isPlcConnected;


        private object? _currentPageViewModel;
        public object? CurrentPageViewModel { get => _currentPageViewModel; set => SetProperty(ref _currentPageViewModel, value); }

        public IRelayCommand ExitApplicationCommand { get; }
        public IRelayCommand NavigateProgramiCommand { get; }
        public IRelayCommand NavigateAdminCommand { get; }
        public IRelayCommand NavigateManualCommand { get; }

        public MainWindowViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _logger = new Logger();
            _plcService = new PlcService();
            _plcClient = new PlcTcpClient(_logger);

            var plcTestViewModel = new PlcTestViewModel(_plcClient, _logger);

            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _plcService, _logger);
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate, plcTestViewModel);

            _mainPageViewModel.PropertyChanged += MainPageViewModel_PropertyChanged;
            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged;

            CurrentPageViewModel = _welcomeViewModel;

            ExitApplicationCommand = new RelayCommand(ExitApplication);
            NavigateProgramiCommand = new RelayCommand(() => Navigate("Run"));
            NavigateAdminCommand = new RelayCommand(() => Navigate("Admin"));
            NavigateManualCommand = new RelayCommand(() => Navigate("Manual"));
        }

        public void Dispose()
        {
            // Stop background polling before disposing shared services
            _mainPageViewModel.StopPolling();

            // Detach event handlers to avoid potential memory leaks
            _mainPageViewModel.PropertyChanged -= MainPageViewModel_PropertyChanged;
            _plcClient.ConnectionStatusChanged -= OnPlcConnectionStatusChanged;
            _welcomeViewModel.Dispose();

            // Dispose managed resources
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

        private void Navigate(string destination)
        {
            switch (destination)
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
                case "Manual":
                    // Placeholder for future manual mode view
                    break;
            }
        }

        private void MainPageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPageViewModel.PlcStatusText))
            {
                PlcStatusText = _mainPageViewModel.PlcStatusText;
            }
        }

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            IsPlcConnected = isConnected;
            if (!isConnected)
                PlcStatusText = "PLC ni povezan";
        }
    }
}