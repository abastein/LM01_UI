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
        private Recipe? _selectedRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _selectedRecipeSteps = new();

        [ObservableProperty]
        private string _plcStatusText = "Povezava ni vzpostavljena";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartPlcCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopPlcCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        private bool _isPlcConnected;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartPlcCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        private bool _isRecipeLoaded;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartPlcCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopPlcCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleStartStopCommand))]
        [NotifyPropertyChangedFor(nameof(StartStopButtonText))]
        [NotifyPropertyChangedFor(nameof(StartStopButtonBrush))]
        private bool _isRunning;

        public MainPageViewModel(ApplicationDbContext dbContext,
                                 PlcTcpClient tcpClient,
                                 PlcService plcService,
                                 Logger logger)
        {
            _dbContext = dbContext;
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;

            LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
            LoadRecipeCommand = new AsyncRelayCommand(
                                      LoadRecipeOnPlcAsync,
                                      () => SelectedRecipe != null && IsPlcConnected && !IsRunning);

            StartPlcCommand = new AsyncRelayCommand(
                                      StartPlcAsync,
                                      () => IsPlcConnected && IsRecipeLoaded && !IsRunning);

            StopPlcCommand = new AsyncRelayCommand(
                                      StopPlcAsync,
                                      () => IsPlcConnected && IsRunning);

            ToggleStartStopCommand = new AsyncRelayCommand(
                                          ToggleStartStopAsync,
                                          () => IsPlcConnected && (IsRecipeLoaded || IsRunning));

            ClearSelectionCommand = new RelayCommand(
                                        ClearSelection,
                                        () => SelectedRecipe != null);

            _tcpClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            _ = LoadRecipesAsync();
        }

        /* ---------- UI-bound properties ---------- */

        public string StartStopButtonText => IsRunning ? "Stop" : "Start";
        public IBrush StartStopButtonBrush => IsRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;

        /* ---------- Commands exposed to the view ---------- */

        public IAsyncRelayCommand LoadRecipesCommand { get; }
        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IAsyncRelayCommand StartPlcCommand { get; }
        public IAsyncRelayCommand StopPlcCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IRelayCommand ClearSelectionCommand { get; }

        /* ---------- Property change hooks ---------- */

        partial void OnSelectedRecipeChanged(Recipe? value)
        {
            _ = LoadStepsForSelectedRecipeAsync();
        }

        /* ---------- Command implementations ---------- */

        private async Task ToggleStartStopAsync()
        {
            if (IsRunning)
                await StopPlcAsync();
            else
                await StartPlcAsync();
        }

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipes = await _dbContext.Recipes
                                              .OrderBy(r => r.Name)
                                              .ToListAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                    Recipes = new ObservableCollection<Recipe>(recipes));
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}");
            }
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() => SelectedRecipeSteps.Clear());

            if (SelectedRecipe != null)
            {
                var recipeWithSteps = await _dbContext.Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == SelectedRecipe.Id);

                if (recipeWithSteps != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        foreach (var step in recipeWithSteps.Steps.OrderBy(s => s.StepNumber))
                            SelectedRecipeSteps.Add(step);
                    });
                }
            }
        }

        private void ClearSelection() => SelectedRecipe = null;

        private async Task LoadRecipeOnPlcAsync()
        {
            if (SelectedRecipe == null) return;

            try
            {
                string loadCommand = _plcService.BuildLoadCommand(SelectedRecipe);
                await _tcpClient.SendAsync(loadCommand);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju LOAD: {ex.Message}");
            }
        }

        private async Task StartPlcAsync()
        {
            try
            {
                await _tcpClient.SendAsync(PlcService.StartCommand);
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
                await _tcpClient.SendAsync(PlcService.StopCommand);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju STOP: {ex.Message}");
            }
        }

        /* ---------- PLC status polling ---------- */

        private void OnConnectionStatusChanged(bool isConnected)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPlcConnected = isConnected;

                if (isConnected)
                {
                    PlcStatusText = "Povezan";
                    StartPlcStatusPolling();
                }
                else
                {
                    PlcStatusText = "Povezava prekinjena";
                    IsRecipeLoaded = false;
                    IsRunning = false;
                    StopPlcStatusPolling();
                }
            });
        }

        private void StartPlcStatusPolling()
        {
            if (_pollingCts != null) return;

            _pollingCts = new CancellationTokenSource();
            var token = _pollingCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        string response = await _tcpClient.SendReceiveAsync(
                                              PlcService.StatusCommand,
                                              TimeSpan.FromSeconds(2));

                        if (!string.IsNullOrEmpty(response) && response.Length >= 10)
                            await ProcessPlcResponse(response);
                        else
                            throw new InvalidOperationException("Neveljaven odgovor PLC.");
                    }
                    catch (Exception)
                    {
                        _tcpClient.Disconnect();
                        break;
                    }

                    await Task.Delay(250, token);
                }
            }, token);
        }

        private async Task ProcessPlcResponse(string response)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var plcState = response.Substring(0, 1);

                int.TryParse(response.Substring(1, 3), out int loadedRecipeId);
                int.TryParse(response.Substring(4, 3), out int currentStepId);
                var errorCode = response.Substring(7, 3);

                IsRecipeLoaded = plcState is "1" or "2";
                IsRunning = plcState == "2";

                PlcStatusText = plcState switch
                {
                    "0" => "Pripravljen (Standby)",
                    "1" => $"Receptura naložena (ID: {loadedRecipeId})",
                    "2" => $"Izvajanje… (Korak: {currentStepId})",
                    "3" => $"NAPAKA (Koda: {errorCode})",
                    _ => "Neznano stanje"
                };
            });
        }

        private void StopPlcStatusPolling()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
            _pollingCts = null;
        }
    }
}
