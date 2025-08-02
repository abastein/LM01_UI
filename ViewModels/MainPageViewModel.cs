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

        [ObservableProperty]
        private string _lastStatusResponse = string.Empty;

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
        private bool _isRunning;

        [ObservableProperty]
        private string _startStopButtonText = "Start";

        [ObservableProperty]
        private IBrush _startStopButtonBrush = Brushes.MediumSeaGreen;

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

            // ========================= Revision Start =========================
            // The command constructor now takes a second argument: a function
            // that determines if the command can execute. The UI button's
            // IsEnabled state will automatically reflect this.
            LoadRecipeCommand = new AsyncRelayCommand(LoadRecipeOnPlcAsync, () => CanLoadRecipe);
            ClearSelectionCommand = new AsyncRelayCommand(ClearSelectionAsync, () => CanClearRecipe);
            ToggleStartStopCommand = new AsyncRelayCommand(ToggleStartStopAsync, () => CanToggleRunning);
            // ========================== Revision End ==========================

            _tcpClient.ConnectionStatusChanged += isConnected =>
            {
                Dispatcher.UIThread.InvokeAsync(() => IsPlcConnected = isConnected);
                StartPlcStatusPolling();
                _ = RefreshStatusAsync();
            };

            if (_tcpClient.IsConnected)
            {
                IsPlcConnected = true;
                StartPlcStatusPolling();
            }
            _ = LoadRecipesAsync();
        }

        // ========================= Revision Start =========================
        // The partial methods now notify the commands to re-evaluate their
        // CanExecute status, which will enable/disable the buttons in the UI.

        partial void OnSelectedRecipeChanged(Recipe? oldValue, Recipe? newValue)
        {
            LoadRecipeCommand.NotifyCanExecuteChanged();
            _ = LoadStepsForSelectedRecipeAsync();
        }

        partial void OnIsPlcConnectedChanged(bool oldValue, bool newValue)
        {
            LoadRecipeCommand.NotifyCanExecuteChanged();
            ToggleStartStopCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsRecipeLoadedChanged(bool oldValue, bool newValue)
        {
            ClearSelectionCommand.NotifyCanExecuteChanged();
            ToggleStartStopCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsRunningChanged(bool oldValue, bool newValue)
        {
            LoadRecipeCommand.NotifyCanExecuteChanged();
            ClearSelectionCommand.NotifyCanExecuteChanged();
            ToggleStartStopCommand.NotifyCanExecuteChanged();

            StartStopButtonText = IsRunning ? "Stop" : "Start";
            StartStopButtonBrush = IsRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;
        }
        // ========================== Revision End ==========================

        // ... The rest of your file remains unchanged ...

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
                await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = response);
                await ProcessPlcResponse(response);
                await RefreshStatusAsync();
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
                await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = response);
                await ProcessPlcResponse(response);
                SelectedRecipe = null;
                await RefreshStatusAsync();
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
                await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = response);
                await ProcessPlcResponse(response);
                await RefreshStatusAsync();
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
                await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = response);
                await ProcessPlcResponse(response);
                await RefreshStatusAsync();
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

        public async Task RefreshStatusAsync()
        {
            if (!_tcpClient.IsConnected)
                return;

            try
            {
                string statusResponse = await _tcpClient.SendReceiveAsync(
                    _plcService.GetStatusCommand(),
                    TimeSpan.FromSeconds(0.5));
                await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = statusResponse);
                await ProcessPlcResponse(statusResponse);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Status refresh failed: {ex.Message}");
            }
        }

        public void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
        }

        private async Task PollStatusLoop(CancellationToken token)
        {

            try
            {
                while (!token.IsCancellationRequested)
                {
                    string? response = null;
                    try
                    {
                        response = await _tcpClient.SendReceiveAsync(
                            _plcService.GetStatusCommand(),
                            TimeSpan.FromSeconds(0.5));
                    }
                    catch (TimeoutException)
                    {
                        _logger.Inform(2, "Status polling timed out.");
                    }
                    catch (Exception ex)
                    {
                        _logger.Inform(2, $"Status polling failed: {ex.Message}");
                        _tcpClient.Disconnect();
                        break;
                    }

                    if (response != null && response != LastStatusResponse)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => LastStatusResponse = response);
                        await ProcessPlcResponse(response);
                    }

                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException) { /* Polling was canceled. */ }
            finally
            {
                StopPolling();
            }
        }

        private async Task ProcessPlcResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return;

            var digits = new string(response.Where(char.IsDigit).ToArray());
            if (digits.Length >= 10)
                digits = digits[^10..];

            if (digits.Length < 10)
                return;

            string plcState = digits.Substring(0, 1);
            int.TryParse(digits.Substring(1, 3), out int loadedId);
            int.TryParse(digits.Substring(4, 2), out int step);
            int.TryParse(digits.Substring(6, 4), out int err);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var recipeStep in SelectedRecipeSteps)
                {
                    recipeStep.IsActive = step > 0 && recipeStep.StepNumber == step;
                }

                CurrentStepNumber = step;
                PlcErrorCode = err;

                IsRecipeLoaded = plcState is "1" or "2" or "3";
                IsRunning = plcState == "2";

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