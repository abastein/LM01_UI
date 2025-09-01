using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using LM01_UI.Enums;
using LM01_UI.Models;
using LM01_UI.Services;
using System;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;

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
        private DirectionType _direction = DirectionType.CW;

        [ObservableProperty]
        private bool _isLoaded;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _loadUnloadText = "Load";

        [ObservableProperty]
        private string _startStopText = "Start";

        public IRelayCommand IncreaseRpmCommand { get; }
        public IRelayCommand DecreaseRpmCommand { get; }
        public IAsyncRelayCommand ToggleLoadUnloadCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IAsyncRelayCommand RunCommand { get; }

        public ManualModeViewModel(PlcTcpClient tcpClient, PlcService plcService, Logger logger)
        {
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            IncreaseRpmCommand = new RelayCommand(() => Rpm++);
            DecreaseRpmCommand = new RelayCommand(() => { if (Rpm > 0) Rpm--; });

            ToggleLoadUnloadCommand = new AsyncRelayCommand(ToggleLoadUnloadAsync);
            ToggleStartStopCommand = new AsyncRelayCommand(ToggleStartStopAsync);
            RunCommand = new AsyncRelayCommand(RunAsync);
        }

        private async Task RunAsync()
        {
            try
            {
                await _tcpClient.SendAsync(_plcService.GetManualLoadCommand(Rpm, Direction));
                await _tcpClient.SendAsync(_plcService.GetStartCommand());
                IsLoaded = true;
                IsRunning = true;
                LoadUnloadText = "Unload";
                StartStopText = "Stop";
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Error running spindle: {ex.Message}");
            }
        }

        private async Task ToggleLoadUnloadAsync()
        {
            try
            {
                if (IsLoaded)
                {
                    await _tcpClient.SendAsync(_plcService.GetUnloadCommand());
                    IsLoaded = false;
                }
                else
                {
                    var step = new RecipeStep
                    {
                        StepNumber = 1,
                        Function = FunctionType.Rotate,
                        SpeedRPM = Rpm,
                        Direction = Direction,
                        TargetXDeg = 0,
                        Repeats = 1,
                        PauseMs = 0
                    };
                    var recipe = new Recipe { Id = 0 };
                    recipe.Steps.Add(step);
                    await _tcpClient.SendAsync(_plcService.BuildLoadCommand(recipe));
                    IsLoaded = true;
                }
                LoadUnloadText = IsLoaded ? "Unload" : "Load";
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Error toggling load: {ex.Message}");
            }
        }

        private async Task ToggleStartStopAsync()
        {
            try
            {
                if (IsRunning)
                {
                    await _tcpClient.SendAsync(_plcService.GetStopCommand());
                    IsRunning = false;
                }
                else
                {
                    await _tcpClient.SendAsync(_plcService.GetStartCommand());
                    IsRunning = true;
                }
                StartStopText = IsRunning ? "Stop" : "Start";
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Error toggling start: {ex.Message}");
            }
        }
    }
}
