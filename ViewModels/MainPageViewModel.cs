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
        private readonly PlcStatusService _statusService;
        private readonly Logger _logger;
        private readonly SemaphoreSlim _statusUpdateSemaphore = new(1, 1);
        private bool _acceptPlcUpdates;
        private bool _initialStatusProcessed;

        [ObservableProperty]
        private string _lastStatusResponse = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        private Recipe? _selectedRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _selectedRecipeSteps = new();

        [ObservableProperty]
        private RecipeStep? _selectedStep;

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

        //[ObservableProperty]
        //private Recipe? _plcLoadedRecipe;

        [ObservableProperty]
        private int? _loadedRecipeId;

        public bool CanLoadRecipe => SelectedRecipe != null && IsPlcConnected && !IsRunning && !IsRecipeLoaded;
        public bool CanClearRecipe => IsRecipeLoaded && !IsRunning;
        public bool CanToggleRunning => IsPlcConnected && (IsRecipeLoaded || IsRunning);
        public bool IsSelectionEnabled => !IsRecipeLoaded;

        //public Recipe? ActiveRecipe
        //{
        //    get => PlcLoadedRecipe ?? SelectedRecipe;
        //    set => SelectedRecipe = value;
        //}


        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IAsyncRelayCommand ToggleStartStopCommand { get; }
        public IAsyncRelayCommand ClearSelectionCommand { get; }

        public MainPageViewModel(
            ApplicationDbContext dbContext,
            PlcTcpClient tcpClient,
            PlcService plcService,
            PlcStatusService statusService,
            Logger logger)
        {
            _dbContext = dbContext;
            _tcpClient = tcpClient;
            _plcService = plcService;
            _statusService = statusService;
            _logger = logger;

            LoadRecipeCommand = new AsyncRelayCommand(LoadRecipeOnPlcAsync, () => CanLoadRecipe);
            ClearSelectionCommand = new AsyncRelayCommand(ClearSelectionAsync, () => CanClearRecipe);
            ToggleStartStopCommand = new AsyncRelayCommand(ToggleStartStopAsync, () => CanToggleRunning);
     

            _tcpClient.ConnectionStatusChanged += isConnected =>
            {
                IsPlcConnected = isConnected;
                if (isConnected) _statusService.Start();
                else _statusService.Stop();
            };

            _statusService.StatusUpdated += async (sender, e) => await OnStatusUpdated(sender, e);

            _ = LoadRecipesAsync();
        }

        partial void OnSelectedRecipeChanged(Recipe? oldValue, Recipe? newValue)
        {
            LoadRecipeCommand.NotifyCanExecuteChanged();
            _ = LoadStepsForSelectedRecipeAsync();

            foreach (var recipe in Recipes)
            {
                recipe.IsActive = recipe == SelectedRecipe;
            }
        }

        partial void OnIsPlcConnectedChanged(bool oldValue, bool newValue)
        {
            LoadRecipeCommand.NotifyCanExecuteChanged();
            ToggleStartStopCommand.NotifyCanExecuteChanged();
        }

        private void UpdateUiState()
        {
            IsRecipeLoaded = LoadedRecipeId.HasValue;
            OnPropertyChanged(nameof(IsSelectionEnabled));
            LoadRecipeCommand.NotifyCanExecuteChanged();
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

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipes = await _dbContext.Recipes
                    .OrderBy(r => r.Id)
                    .ToListAsync();
                Recipes = new ObservableCollection<Recipe>(recipes);
                foreach (var recipe in Recipes)
                {
                    recipe.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}");
            }
        }

        public Task ReloadRecipesAsync()
        {
            return LoadRecipesAsync();
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            SelectedRecipeSteps.Clear();
            var recipe = SelectedRecipe;
            if (recipe != null)
            {
                var recipeWithSteps = await _dbContext.Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == recipe.Id);
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
                await _tcpClient.SendAsync(command);
                _acceptPlcUpdates = true;
                LoadedRecipeId = SelectedRecipe.Id;
                UpdateUiState();
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
                await _tcpClient.SendAsync(_plcService.GetUnloadCommand());
                _acceptPlcUpdates = false;
                LoadedRecipeId = null;
                UpdateUiState();
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
                await _tcpClient.SendAsync(_plcService.GetStartCommand());
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
                await _tcpClient.SendAsync(_plcService.GetStopCommand());
                var stopped = await WaitForStateAsync("1", TimeSpan.FromSeconds(5));
                if (stopped)
                {
                    IsRunning = false;
                    UpdateUiState();
                }
                else
                {
                    _logger.Inform(2, "PLC did not confirm stop state in time");
                }
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju STOP: {ex.Message}");
            }
        }

        private async Task<bool> WaitForStateAsync(string expectedState, TimeSpan timeout)
        {
            var command = _plcService.GetStatusCommand();
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var response = await _tcpClient.SendReceiveAsync(command, TimeSpan.FromSeconds(0.25));
                    var digits = new string(response.Where(char.IsDigit).ToArray());
                    if (digits.Length > 0 && digits[0].ToString() == expectedState)
                    {
                        return true;
                    }
                }
                catch (TimeoutException)
                {
                    // retry until timeout
                }
                await Task.Delay(100);
            }
            return false;
        }


        private async Task OnStatusUpdated(object? sender, PlcStatusEventArgs e)
        {
            await _statusUpdateSemaphore.WaitAsync();
            try
            {
                var status = e.Status;
                _logger.Inform(1, $"MainPageViewModel.OnStatusUpdated start: State={status.State}, LoadedRecipeId={status.LoadedRecipeId}, Step={status.Step}, ErrorCode={status.ErrorCode}");

                if (!_initialStatusProcessed)
                {
                    _initialStatusProcessed = true;
                    if (status.LoadedRecipeId == 999)
                    {
                        try
                        {
                            await _tcpClient.SendAsync(_plcService.GetUnloadCommand());
                        }
                        catch (Exception ex)
                        {
                            _logger.Inform(2, $"Napaka pri pošiljanju UNLOAD: {ex.Message}");
                        }
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            LoadedRecipeId = null;
                            UpdateUiState();
                        });
                        return;
                    }
                }


                Recipe? recipe = null;
                try
                {
                    recipe = await _dbContext.Recipes
                        .Include(r => r.Steps)
                        .FirstOrDefaultAsync(r => r.Id == status.LoadedRecipeId);
                }
                catch (Exception ex)
                {
                    _logger.Inform(2, $"Napaka pri pridobivanju recepture: {ex.Message}");
                }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LastStatusResponse = status.Raw;

                    if (_acceptPlcUpdates)
                    {
                        if (status.State is "1" or "2" or "3")
                        {
                            var existing = Recipes.FirstOrDefault(r => r.Id == status.LoadedRecipeId);
                            if (existing is null && recipe is not null)
                            {
                                Recipes.Add(recipe);
                                existing = recipe;
                            }
                            SelectedRecipe = existing;
                        }
                        else
                        {
                            SelectedRecipe = null;

                        }

                    OnPropertyChanged(nameof(SelectedRecipe));
                    foreach (var r in Recipes)
                        {
                            r.IsActive = r == SelectedRecipe;
                        }

                        LoadedRecipeId = status.State is "1" or "2" or "3" ? status.LoadedRecipeId : (int?)null;
                        IsRunning = status.State == "2";
                        UpdateUiState();
                    }

                    foreach (var recipeStep in SelectedRecipeSteps)
                        {
                            recipeStep.IsActive = status.Step > 0 && recipeStep.StepNumber == status.Step;
                        }

                CurrentStepNumber = status.Step;
                PlcErrorCode = status.ErrorCode;

                    PlcStatusText = status.State switch
                    {
                        "1" => $"Receptura naložena (ID: {status.LoadedRecipeId})",
                        "2" => $"Izvajanje… (Receptura: {status.LoadedRecipeId}, Korak: {status.Step})",
                        "3" => $"NAPAKA (Koda: {status.ErrorCode})",
                        _ => PlcStatusText
                    };
                });

                _logger.Inform(1, $"MainPageViewModel.OnStatusUpdated end: State={status.State}, LoadedRecipeId={status.LoadedRecipeId}, Step={status.Step}, ErrorCode={status.ErrorCode}");

            }
            finally
            {
                _statusUpdateSemaphore.Release();
            }

        }

    }
}