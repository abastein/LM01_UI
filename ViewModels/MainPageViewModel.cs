using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _tcpClient;
        private readonly PlcService _plcService;
        private readonly Logger _logger;
        private CancellationTokenSource? _pollingCts;
        private string? _lastStatusResponse;

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        private Recipe? _selectedRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _selectedRecipeSteps = new();

        [ObservableProperty]
        private string _plcStatusText = "Povezava ni vzpostavljena";

        [ObservableProperty]
        private bool _isPlcConnected;

        [ObservableProperty]
        private bool _isRecipeLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StartStopButtonText))]
        [NotifyPropertyChangedFor(nameof(StartStopButtonBrush))]
        private bool _isRunning;

        [ObservableProperty]
        private int _currentStepNumber;

        [ObservableProperty]
        private int _plcErrorCode;

        [ObservableProperty]
        private int? _loadedRecipeId;

        /// <summary>
        /// Controls button enabled state in the UI.
        /// </summary>
        public bool CanLoadRecipe => SelectedRecipe != null && IsPlcConnected && !IsRunning;
        public bool CanClearRecipe => IsRecipeLoaded && !IsRunning;
        public bool CanToggleRunning => IsPlcConnected && (IsRecipeLoaded || IsRunning);

        /// <summary>
        /// Text displayed on Start/Stop button.
        /// </summary>
        public string StartStopButtonText => IsRunning ? "Stop" : "Start";

        /// <summary>
        /// Background brush for Start/Stop button.
        /// </summary>
        public IBrush StartStopButtonBrush => IsRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;

        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IAsyncRelayCommand ClearSelectionCommand { get; }

        public MainPageViewModel(
            ApplicationDbContext dbContext,
            PlcTcpClient tcpClient,
            PlcService plcService,
            Logger logger)
        {
            _dbContext = dbContext;
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            LoadRecipeCommand = new AsyncRelayCommand(LoadRecipeOnPlcAsync);
            ClearSelectionCommand = new AsyncRelayCommand(ClearSelectionAsync);
            ToggleStartStopCommand = new AsyncRelayCommand(ToggleStartStopAsync);

            _tcpClient.ConnectionStatusChanged += isConnected =>
            {
                Dispatcher.UIThread.InvokeAsync(() => IsPlcConnected = isConnected);
                StartPlcStatusPolling();
            };

            _ = LoadRecipesAsync();
        }

        partial void OnSelectedRecipeChanged(Recipe? oldValue, Recipe? newValue)
        {
            OnPropertyChanged(nameof(CanLoadRecipe));
            _ = LoadStepsForSelectedRecipeAsync();
        }

        partial void OnIsPlcConnectedChanged(bool oldValue, bool newValue)
        {
            OnPropertyChanged(nameof(CanLoadRecipe));
            OnPropertyChanged(nameof(CanToggleRunning));
        }

        partial void OnIsRecipeLoadedChanged(bool oldValue, bool newValue)
        {
            OnPropertyChanged(nameof(CanClearRecipe));
            OnPropertyChanged(nameof(CanToggleRunning));
        }

        partial void OnIsRunningChanged(bool oldValue, bool newValue)
        {
            OnPropertyChanged(nameof(CanLoadRecipe));
            OnPropertyChanged(nameof(CanClearRecipe));
            OnPropertyChanged(nameof(CanToggleRunning));
        }

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipes = await _dbContext.Recipes
                    .OrderBy(r => r.Name)
                    .ToListAsync();
                Recipes = new ObservableCollection<Recipe>(recipes);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}");
            }
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            SelectedRecipeSteps.Clear();
            if (SelectedRecipe != null)
            {
                var recipeWithSteps = await _dbContext.Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == SelectedRecipe.Id);
                if (recipeWithSteps != null)
                {
                    foreach (var step in recipeWithSteps.Steps.OrderBy(s => s.StepNumber))
                        SelectedRecipeSteps.Add(step);
                }
            }
        }

        private async Task LoadRecipeOnPlcAsync()
        {
            if (SelectedRecipe == null)
                return;

            try
            {
                string command = _plcService.BuildLoadCommand(SelectedRecipe);
                string response = await _tcpClient.SendReceiveAsync(command, TimeSpan.FromSeconds(2));
                _lastStatusResponse = response;
                await ProcessPlcResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju LOAD: {ex.Message}");
            }
        }

        private async Task ClearSelectionAsync()
        {
            if (!IsRecipeLoaded) return;
            try
            {
                string response = await _tcpClient.SendReceiveAsync(_plcService.GetUnloadCommand(), TimeSpan.FromSeconds(2));
                _lastStatusResponse = response;
                await ProcessPlcResponse(response);
                SelectedRecipe = null;
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju UNLOAD: {ex.Message}");
            }
        }

        private async Task ToggleStartStopAsync()
        {
            if (IsRunning)
                await StopPlcAsync();
            else
                await StartPlcAsync();
        }

        private async Task StartPlcAsync()
        {
            try
            {
                string response = await _tcpClient.SendReceiveAsync(_plcService.GetStartCommand(), TimeSpan.FromSeconds(2));
                _lastStatusResponse = response;
                await ProcessPlcResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju START: {ex.Message}");
            }
        }

        private async Task StopPlcAsync()
        {
            try
            {
                string response = await _tcpClient.SendReceiveAsync(_plcService.GetStopCommand(), TimeSpan.FromSeconds(2));
                _lastStatusResponse = response;
                await ProcessPlcResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju STOP: {ex.Message}");
            }
        }

        public void StartPlcStatusPolling()
        {
            if (_pollingCts != null || !_tcpClient.IsConnected)
                return;
            _pollingCts = new CancellationTokenSource();
            _ = PollStatusLoop(_pollingCts.Token);
        }

        public void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
        }

        private async Task PollStatusLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string response;
                try
                {
                    response = await _tcpClient.SendReceiveAsync(
                        _plcService.GetStatusCommand(),
                        TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    // If the PLC doesn't reply in time, keep the connection
                    // open and try again on the next iteration.
                    _logger.Inform(2, "Status polling timed out.");
                    await Task.Delay(1000, token);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.Inform(2, $"Status polling failed: {ex.Message}");
                    _tcpClient.Disconnect();
                    break;
                }

                if (response != _lastStatusResponse)
                {
                    _lastStatusResponse = response;
                    await ProcessPlcResponse(response);
                }

                await Task.Delay(400, token);
            }
        }

        private async Task ProcessPlcResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return;

            // Strip any non-digit characters and take the last 10 digits
            var digits = new string(response.Where(char.IsDigit).ToArray());
            if (digits.Length >= 10)
                digits = digits[^10..];

            if (digits.Length < 10)
                return;

            // First character indicates state: 0=standby,1=loaded,2=running,3=error
            string plcState = digits.Substring(0, 1);
            int.TryParse(digits.Substring(1, 3), out int loadedId);
            int.TryParse(digits.Substring(4, 2), out int step);
            int.TryParse(digits.Substring(6, 4), out int err);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStepNumber = step;
                PlcErrorCode = err;

                if (plcState != "0")
                {
                    IsRecipeLoaded = plcState is "1" or "2" or "3";
                    IsRunning = plcState == "2";
                }

                LoadedRecipeId = IsRecipeLoaded ? loadedId : null;
                PlcStatusText = plcState switch
                {
                    "1" => $"Receptura naložena (ID: {loadedId})",
                    "2" => $"Izvajanje… (Receptura: {loadedId}, Korak: {step})",
                    "3" => $"NAPAKA (Koda: {err})",
                    _ => PlcStatusText
                };
            });
        }
    }
}
