using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Enums;
//using LM01_UI.Models;
using LM01_UI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;

namespace LM01_UI.ViewModels
{
    public partial class ManualModeViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _tcpClient;
        private readonly PlcService _plcService;
        private readonly Logger _logger;

        [ObservableProperty]
        private int _rpm;

        [ObservableProperty]
        private int _jogDistance = 360;

        [ObservableProperty]
        private DirectionType _direction = DirectionType.CW;

        [ObservableProperty]
        private bool _isLoaded;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _startStopText = "Start";

        [ObservableProperty]
        private IBrush _startStopBrush = Brushes.MediumSeaGreen;
        public bool ControlsEnabled => !IsRunning;

        public IRelayCommand IncreaseRpmCommand { get; }
        public IRelayCommand DecreaseRpmCommand { get; }
        public IRelayCommand IncreaseRpmBy10Command { get; }
        public IRelayCommand DecreaseRpmBy10Command { get; }
        public IAsyncRelayCommand ToggleRunCommand { get; }

        public ManualModeViewModel(PlcTcpClient tcpClient, PlcService plcService, Logger logger)
        {
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            IncreaseRpmCommand = new RelayCommand(() => { if (Rpm < 400) Rpm++; });
            DecreaseRpmCommand = new RelayCommand(() => { if (Rpm > 0) Rpm--; });
            IncreaseRpmBy10Command = new RelayCommand(() => { if (Rpm < 400) Rpm = Math.Min(400, Rpm + 10); });
            DecreaseRpmBy10Command = new RelayCommand(() => { if (Rpm > 0) Rpm = Math.Max(0, Rpm - 10); });

            ToggleRunCommand = new AsyncRelayCommand(ToggleRunAsync);
        }

        private async Task ToggleRunAsync()
        {
            try
            {
                if (!IsLoaded || !IsRunning)
                {
                    await _tcpClient.SendAsync(_plcService.GetManualLoadCommand(Rpm, Direction, JogDistance)); 
                    var loaded = await WaitForStateAsync("1", TimeSpan.FromSeconds(5));
                    if (!loaded)
                    {
                        _logger.Inform(2, "PLC did not confirm load state in time");
                    }
                    await _tcpClient.SendAsync(_plcService.GetStartCommand());
                    IsLoaded = true;
                    IsRunning = true;
                    StartStopText = "Stop";
                    StartStopBrush = Brushes.IndianRed;
                }
                else
                {
                    await _tcpClient.SendAsync(_plcService.GetStopCommand());
                    var stopped = await WaitForStateAsync("1", TimeSpan.FromSeconds(5));
                    if (!stopped)
                    {
                        _logger.Inform(2, "PLC did not confirm stop state in time");
                    }
                    await _tcpClient.SendAsync(_plcService.GetUnloadCommand());
                    IsLoaded = false;
                    IsRunning = false;
                    StartStopText = "Start";
                    StartStopBrush = Brushes.MediumSeaGreen;
                }

            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Error toggling run: {ex.Message}");
            }
        }
        private async Task<bool> WaitForStateAsync(string expectedState, TimeSpan timeout)
        {
            var command = _plcService.GetStatusCommand();
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var response = await _tcpClient.SendReceiveAsync(command, TimeSpan.FromSeconds(0.25));
                if (response is not null)
                {
                    var digits = new string(response.Where(char.IsDigit).ToArray());
                    if (digits.Length > 0 && digits[0].ToString() == expectedState)
                    {
                        return true;
                    }
                }
                await Task.Delay(100);
            }
            return false;
        }
        partial void OnRpmChanged(int value)
        {
            if (value > 400)
            {
                _rpm = 400;
                OnPropertyChanged(nameof(Rpm));
            }
            else if (value < 0)
            {
                _rpm = 0;
                OnPropertyChanged(nameof(Rpm));
            }
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(ControlsEnabled));
        }

        public void ResetToStartupState()
        {
            Rpm = 0;
            JogDistance = 360;
            Direction = DirectionType.CW;
            IsLoaded = false;
            IsRunning = false;
            StartStopText = "Start";
            StartStopBrush = Brushes.MediumSeaGreen;
        }
    }
}
