using LM01_UI.Data.Persistence;
using LM01_UI.Services;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using Avalonia.Threading;
using System.Threading.Tasks;
//using LM01_UI.Views;

namespace LM01_UI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly PlcService _plcService;
        private readonly PlcStatusService _plcStatusService;
        private readonly Logger _logger;
       // private readonly DebugLogViewModel _debugLogViewModel;
       // private readonly Window _debugLogWindow;
        private readonly WelcomeViewModel _welcomeViewModel;
        private readonly AdminPageViewModel _adminPageViewModel;
        private readonly MainPageViewModel _mainPageViewModel;
        private readonly ManualModeViewModel _manualModeViewModel;

        [ObservableProperty]
        private string _plcStatusText = "Pripravljen ...";

        [ObservableProperty]
        private bool _isPlcConnected;

        [ObservableProperty]
        private string _lastStatusResponse = string.Empty;


        private bool _startupStatusHandled;
        private object? _currentPageViewModel;
        public object? CurrentPageViewModel { get => _currentPageViewModel; set => SetProperty(ref _currentPageViewModel, value); }

        public IRelayCommand ExitApplicationCommand { get; }
        public IRelayCommand NavigateProgramiCommand { get; }
        public IRelayCommand NavigateAdminCommand { get; }
        public IRelayCommand NavigateManualCommand { get; }
        public IRelayCommand NavigatePranjeCommand { get; }

        public MainWindowViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _logger = new Logger();
            _plcService = new PlcService();
            _plcClient = new PlcTcpClient(_logger);
            _plcStatusService = new PlcStatusService(_plcClient, _plcService, _logger);

            var plcTestViewModel = new PlcTestViewModel(_plcClient, _logger);

            _welcomeViewModel = new WelcomeViewModel(_plcClient, _logger, Navigate);
            _mainPageViewModel = new MainPageViewModel(_dbContext, _plcClient, _plcService, _plcStatusService, _logger);
            _adminPageViewModel = new AdminPageViewModel(_plcClient, _logger, _dbContext, Navigate, plcTestViewModel);
            _manualModeViewModel = new ManualModeViewModel(_plcClient, _plcService, _logger);

            _plcStatusService.StatusUpdated += OnStatusUpdated;
            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged;


            // Establish PLC connection after all view models subscribed to
            // connection events so that polling and status updates start correctly.
            _welcomeViewModel.ConnectToPlcCommand.Execute(null);

            CurrentPageViewModel = _welcomeViewModel;

            ExitApplicationCommand = new RelayCommand(ExitApplication);
            NavigateProgramiCommand = new RelayCommand(() => Navigate("Run"));
            NavigateAdminCommand = new RelayCommand(() => Navigate("Admin"));
            NavigateManualCommand = new RelayCommand(() => Navigate("Manual"));
            NavigatePranjeCommand = new RelayCommand(() => Navigate("Pranje"));
        }

        public void Dispose()
        {
            _plcStatusService.StatusUpdated -= OnStatusUpdated;
            _plcClient.ConnectionStatusChanged -= OnPlcConnectionStatusChanged;
            _welcomeViewModel.Dispose();

            _plcStatusService.Dispose();
            _plcClient.Dispose();
            _logger.Dispose();
          //  _debugLogWindow.Close();
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
                    _ = _mainPageViewModel.ReloadRecipesAsync();
                    CurrentPageViewModel = _mainPageViewModel;
                    break;
                case "Admin":
                    CurrentPageViewModel = _adminPageViewModel;
                    break;
                case "Welcome":
                    CurrentPageViewModel = _welcomeViewModel;
                    break;
                case "Manual":
                    CurrentPageViewModel = _manualModeViewModel;
                    break;
                case "Pranje":
                    // Placeholder for future pranje view
                    break;
            }
        }

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            IsPlcConnected = isConnected;
            if (!isConnected)
                PlcStatusText = "PLC ni povezan";
        }

        private async void OnStatusUpdated(object? sender, PlcStatusEventArgs e)
        {
            var status = e.Status;
            _logger.Inform(1, $"MainWindowViewModel.OnStatusUpdated start: State={status.State}, LoadedRecipeId={status.LoadedRecipeId}, Step={status.Step}, ErrorCode={status.ErrorCode}");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LastStatusResponse = status.Raw;
                PlcStatusText = status.State switch
                {
                    "1" => $"Receptura naložena (ID: {status.LoadedRecipeId})",
                    "2" => $"Izvajanje… (Receptura: {status.LoadedRecipeId}, Korak: {status.Step})",
                    "3" => $"NAPAKA (Koda: {status.ErrorCode})",
                    _ => PlcStatusText
                };

                if (!_startupStatusHandled && (status.State == "1" || status.State == "2"))
                {
                    Navigate("Run");
                    _startupStatusHandled = true;
                }
            });


            _logger.Inform(1, $"MainWindowViewModel.OnStatusUpdated end: State={status.State}, LoadedRecipeId={status.LoadedRecipeId}, Step={status.Step}, ErrorCode={status.ErrorCode}");
        }
    }
}