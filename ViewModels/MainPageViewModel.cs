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

        [ObservableProperty]
        private int _currentStepNumber;

        [ObservableProperty]
        private int _plcErrorCode;


        public MainPageViewModel(ApplicationDbContext dbContext,
                                 PlcTcpClient tcpClient,
                                 Logger logger)
        {
            _dbContext = dbContext;
            _tcpClient = tcpClient;
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
            OnConnectionStatusChanged(_tcpClient.IsConnected);

            _ = LoadRecipesAsync();
        }

        public string StartStopButtonText => IsRunning ? "Stop" : "Start";
        public IBrush StartStopButtonBrush => IsRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;
        public IAsyncRelayCommand LoadRecipesCommand { get; }
        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IAsyncRelayCommand StartPlcCommand { get; }
        public IAsyncRelayCommand StopPlcCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IRelayCommand ClearSelectionCommand { get; }


        partial void OnSelectedRecipeChanged(Recipe? value)
        {
            IsRecipeLoaded = false;
            _ = LoadStepsForSelectedRecipeAsync();
        }

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
                var recipes = await _dbContext.Recipes.OrderBy(r => r.Name).ToListAsync();
                await Dispatcher.UIThread.InvokeAsync(() =>
                    Recipes = new ObservableCollection<Recipe>(recipes));

                StartPlcStatusPolling();
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}"); }
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() => SelectedRecipeSteps.Clear());
            if (SelectedRecipe != null)
            {
                var recipeWithSteps = await _dbContext.Recipes.Include(r => r.Steps).FirstOrDefaultAsync(r => r.Id == SelectedRecipe.Id);
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
                string loadCommand = PlcService.BuildLoadCommand(SelectedRecipe);
                // Ukaz pošljemo in takoj počakamo na odgovor za posodobitev vmesnika
                string response = await _tcpClient.SendReceiveAsync(loadCommand, TimeSpan.FromSeconds(2));
                await ProcessPlcResponse(response);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri pošiljanju LOAD: {ex.Message}"); }
        }

        private async Task StartPlcAsync()
        {
            try
            {
                string response = await _tcpClient.SendReceiveAsync(PlcService.StartCommand, TimeSpan.FromSeconds(2));
                await ProcessPlcResponse(response);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri pošiljanju START: {ex.Message}"); }
        }

        private async Task StopPlcAsync()
        {
            try
            {
                string response = await _tcpClient.SendReceiveAsync(PlcService.StopCommand, TimeSpan.FromSeconds(2));
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
                // Najprej počakamo, da ne pošljemo zahteve takoj po ukazu z gumba
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) break;

                try
                {
                    string response = await _tcpClient.SendReceiveAsync(PlcService.StatusCommand, TimeSpan.FromSeconds(2));
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

            // Razčlenimo odgovor iz PLC-ja
            string plcState = response.Substring(0, 1);
            int.TryParse(response.Substring(1, 3), out int loadedRecipeId);
            int.TryParse(response.Substring(4, 3), out int currentStep);
            int.TryParse(response.Substring(7, 3), out int errorCode);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Posodobimo glavne lastnosti stanja
                CurrentStepNumber = currentStep;
                PlcErrorCode = errorCode;
                IsRecipeLoaded = plcState is "1" or "2" or "9";
                IsRunning = plcState == "2";

                // POPRAVEK: Dodana nova logika za osveževanje aktivnega koraka
                // Gremo čez vse prikazane korake v desni tabeli...
                foreach (var step in SelectedRecipeSteps)
                {
                    // ...in nastavimo IsActive samo za tistega, ki se trenutno izvaja.
                    step.IsActive = (IsRunning && step.StepNumber == CurrentStepNumber);
                }

                // Posodobimo opisno besedilo statusa
                PlcStatusText = plcState switch
                {
                    "0" => "Pripravljen (Standby)",
                    "1" => $"Receptura naložena (ID: {loadedRecipeId})",
                    "2" => $"Izvajanje… (Receptura: {loadedRecipeId}, Korak: {currentStep})",
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