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
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        private Recipe? _selectedRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _selectedRecipeSteps = new();

        [ObservableProperty]
        private string _plcStatusText = "Povezava ni vzpostavljena";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
        private bool _isPlcConnected;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
        private bool _isRecipeLoaded;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        [NotifyPropertyChangedFor(nameof(StartStopButtonText))]
        [NotifyPropertyChangedFor(nameof(StartStopButtonBrush))]
        private bool _isRunning;

        [ObservableProperty]
        private int _currentStepNumber;

        [ObservableProperty]
        private int _plcErrorCode;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        private int? _loadedRecipeId;

        public MainPageViewModel(ApplicationDbContext dbContext, PlcTcpClient tcpClient, PlcService plcService, Logger logger)
        {
            _dbContext = dbContext;
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            LoadRecipeCommand = new AsyncRelayCommand(LoadRecipeOnPlcAsync, () => SelectedRecipe != null && IsPlcConnected && !IsRunning && SelectedRecipe.Id != LoadedRecipeId);
            ToggleStartStopCommand = new AsyncRelayCommand(ToggleStartStopAsync, () => IsPlcConnected && (IsRunning || (IsRecipeLoaded && SelectedRecipe != null && SelectedRecipe.Id == LoadedRecipeId)));
            ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedRecipe != null);

            _tcpClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            OnConnectionStatusChanged(_tcpClient.IsConnected);
            _ = LoadRecipesAsync();
        }

        public string StartStopButtonText => IsRunning ? "Stop" : "Start";
        public IBrush StartStopButtonBrush => IsRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;
        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IRelayCommand ClearSelectionCommand { get; }

        partial void OnSelectedRecipeChanged(Recipe? value)
        {
            // Ko se spremeni izbira, ponastavimo stanje "naloženosti", da se Naloži gumb pravilno obnaša
            IsRecipeLoaded = false;
            LoadedRecipeId = null;
            _ = LoadStepsForSelectedRecipeAsync();
        }

        private async Task ToggleStartStopAsync()
        {
            if (IsRunning) await StopPlcAsync();
            else await StartPlcAsync();
        }

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipes = await _dbContext.Recipes.OrderBy(r => r.Name).ToListAsync();
                Recipes = new ObservableCollection<Recipe>(recipes);
                StartPlcStatusPolling();
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}"); }
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            SelectedRecipeSteps.Clear();
            if (SelectedRecipe != null)
            {
                var recipeWithSteps = await _dbContext.Recipes.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == SelectedRecipe.Id);
                if (recipeWithSteps != null)
                {
                    foreach (var step in recipeWithSteps.Steps.OrderBy(s => s.StepNumber))
                        SelectedRecipeSteps.Add(step);
                }
            }
        }

        private void ClearSelection()
        {
            SelectedRecipe = null;
        }

        private async Task LoadRecipeOnPlcAsync()
        {
            if (SelectedRecipe == null) return;
            try
            {
                string loadCommand = _plcService.BuildLoadCommand(SelectedRecipe);
                string response = await _tcpClient.SendReceiveAsync(loadCommand, TimeSpan.FromSeconds(2));
                await ProcessPlcResponse(response);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri pošiljanju LOAD: {ex.Message}"); }
        }

        private async Task StartPlcAsync()
        {
            try
            {
                // POPRAVEK: Uporaba instance '_plcService'
                string response = await _tcpClient.SendReceiveAsync(_plcService.StartCommand, TimeSpan.FromSeconds(2));
                await ProcessPlcResponse(response);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri pošiljanju START: {ex.Message}"); }
        }

        private async Task StopPlcAsync()
        {
            try
            {
                // POPRAVEK: Uporaba instance '_plcService'
                string response = await _tcpClient.SendReceiveAsync(_plcService.StopCommand, TimeSpan.FromSeconds(2));
                await ProcessPlcResponse(response);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri pošiljanju STOP: {ex.Message}"); }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPlcConnected = isConnected;
                if (!isConnected)
                {
                    PlcStatusText = "Povezava prekinjena";
                    IsRecipeLoaded = false;
                    IsRunning = false;
                    LoadedRecipeId = null;
                    StopPolling();
                }
            });
        }

        private void StartPlcStatusPolling()
        {
            if (_pollingCts != null) return;
            _pollingCts = new CancellationTokenSource();
            _ = PollStatusLoop(_pollingCts.Token);
        }

        private async Task PollStatusLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) break;
                try
                {
                    // POPRAVEK: Uporaba instance '_plcService'
                    string response = await _tcpClient.SendReceiveAsync(_plcService.StatusCommand, TimeSpan.FromSeconds(2));
                    await ProcessPlcResponse(response);
                }
                catch (Exception)
                {
                    _tcpClient.Disconnect();
                    break;
                }
            }
        }

        private async Task ProcessPlcResponse(string response)
        {
            if (string.IsNullOrEmpty(response) || response.Length < 10) return;
            string plcState = response.Substring(0, 1);
            int.TryParse(response.Substring(1, 3), out int loadedRecipeIdFromPlc);
            int.TryParse(response.Substring(4, 3), out int currentStep);
            int.TryParse(response.Substring(7, 3), out int errorCode);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentStepNumber = currentStep;
                PlcErrorCode = errorCode;
                IsRecipeLoaded = plcState is "1" or "2" or "9";
                IsRunning = plcState == "2";
                LoadedRecipeId = IsRecipeLoaded ? loadedRecipeIdFromPlc : null;
                PlcStatusText = plcState switch
                {
                    "0" => "Pripravljen (Standby)",
                    "1" => $"Receptura naložena (ID: {loadedRecipeIdFromPlc})",
                    "2" => $"Izvajanje… (Receptura: {loadedRecipeIdFromPlc}, Korak: {currentStep})",
                    "9" => $"NAPAKA (Koda: {errorCode})",
                    _ => "Neznano stanje PLC-ja"
                };
            });
        }

        public void StopPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
        }
    }
}