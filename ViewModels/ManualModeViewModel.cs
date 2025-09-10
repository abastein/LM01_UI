using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Enums;
//using LM01_UI.Models;
using LM01_UI.Services;
using System;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class ManualModeViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _tcpClient;
        private readonly PlcService _plcService;
        private readonly Logger _logger;
        private const int CommandDelayMs = 500;

        [ObservableProperty]
        private int _rpm;

        [ObservableProperty]
        private DirectionType _direction = DirectionType.CW;

        [ObservableProperty]
        private bool _isLoaded;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _startStopText = "Start";

        public IRelayCommand IncreaseRpmCommand { get; }
        public IRelayCommand DecreaseRpmCommand { get; }
        public IAsyncRelayCommand ToggleRunCommand { get; }

        public ManualModeViewModel(PlcTcpClient tcpClient, PlcService plcService, Logger logger)
        {
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            IncreaseRpmCommand = new RelayCommand(() => Rpm++);
            DecreaseRpmCommand = new RelayCommand(() => { if (Rpm > 0) Rpm--; });

            ToggleRunCommand = new AsyncRelayCommand(ToggleRunAsync);
        }

        private async Task ToggleRunAsync()
        {
            try
            {
                if (!IsLoaded || !IsRunning)
                {
                    await _tcpClient.SendAsync(_plcService.GetManualLoadCommand(Rpm, Direction));
                    await _tcpClient.SendAsync(_plcService.GetStartCommand());
                    await Task.Delay(CommandDelayMs);
                    IsLoaded = true;
                    IsRunning = true;
                    StartStopText = "Stop";
                }
                else
                {
                    await _tcpClient.SendAsync(_plcService.GetStopCommand());
                    await _tcpClient.SendAsync(_plcService.GetUnloadCommand());
                    await Task.Delay(CommandDelayMs);
                    IsLoaded = false;
                    IsRunning = false;
                    StartStopText = "Start";
                }

            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Error toggling run: {ex.Message}");
            }
        }
    }
}
