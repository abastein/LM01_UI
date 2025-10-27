using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Services;
using System.IO;
using System;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase, IDisposable
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
                    var ipAddress = GetPlcIpAddress();
                    await _plcClient.ConnectAsync(ipAddress, 2000);
                }
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri povezovanju s PLC: {ex.Message}");
            }
        }

        private string GetPlcIpAddress()
        {
            try
            {
                var executableDirectory = AppContext.BaseDirectory;
                var configPath = Path.Combine(executableDirectory, "PLCIP.txt");

                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("Datoteka s PLC IP naslovom ni bila najdena.", configPath);
                }

                var ipAddress = File.ReadAllText(configPath).Trim();

                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    throw new InvalidDataException("Datoteka s PLC IP naslovom je prazna.");
                }

                return ipAddress;
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri branju IP naslova PLC: {ex.Message}");
                throw;
            }
        }

        public void ResetToStartupState()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_plcClient.IsConnected)
                {
                    IsPlcConnected = true;
                    PlcStatusText = "PLC Povezan";
                    PlcStatusBrush = Brushes.MediumSeaGreen;
                }
                else
                {
                    IsPlcConnected = false;
                    PlcStatusText = "PLC ni povezan";
                    PlcStatusBrush = Brushes.IndianRed;
                }
            });
        }


        public void Dispose()
        {
            _plcClient.ConnectionStatusChanged -= OnPlcConnectionStatusChanged;
        }
    }
}