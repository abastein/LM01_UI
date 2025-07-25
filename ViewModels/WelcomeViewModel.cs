using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Services;
using System;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly Action<string> _navigate;

        [ObservableProperty]
        private string _plcStatusText = "PLC ni povezan";

        [ObservableProperty]
        private IBrush _plcStatusBrush = Brushes.IndianRed;

        [ObservableProperty]
        private bool _isPlcConnected;

        public WelcomeViewModel(PlcTcpClient plcClient, Logger logger, Action<string> navigate)
        {
            _plcClient = plcClient;
            _logger = logger;
            _navigate = navigate;

            ConnectToPlcCommand = new AsyncRelayCommand(ConnectToPlcAsync);

            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged;

            // THE FIX: Automatically execute the connect command on startup.
            ConnectToPlcCommand.Execute(null);
        }

        public IAsyncRelayCommand ConnectToPlcCommand { get; }

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsPlcConnected = isConnected;

                if (isConnected)
                {
                    PlcStatusText = "PLC Povezan";
                    PlcStatusBrush = Brushes.MediumSeaGreen;
                    _logger.Inform(1, "Povezava s PLC uspešno vzpostavljena.");
                }
                else
                {
                    PlcStatusText = "PLC ni povezan";
                    PlcStatusBrush = Brushes.IndianRed;
                    _logger.Inform(2, "Povezava s PLC prekinjena.");
                }
            });
        }

        [RelayCommand]
        private void NavigateToRun() => _navigate("Run");

        [RelayCommand]
        private void NavigateToAdmin() => _navigate("Admin");

        private async Task ConnectToPlcAsync()
        {
            try
            {
                if (!_plcClient.IsConnected)
                {
                    // You mentioned you changed this line, which is correct.
                    await _plcClient.ConnectAsync("10.100.1.143", 2001);
                }
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri povezovanju s PLC: {ex.Message}");
            }
        }
    }
}