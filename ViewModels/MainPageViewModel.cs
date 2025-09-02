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

        [ObservableProperty]
        private int? _loadedRecipeId;

        public bool CanLoadRecipe => SelectedRecipe != null && IsPlcConnected && !IsRunning && !IsRecipeLoaded;
        public bool CanClearRecipe => IsRecipeLoaded && !IsRunning;
        public bool CanToggleRunning => IsPlcConnected && (IsRecipeLoaded || IsRunning);
        public bool IsSelectionEnabled => !IsRecipeLoaded;

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

            _statusService.StatusUpdated += OnStatusUpdated;

            _ = LoadRecipesAsync();
        }

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
            LoadRecipeCommand.NotifyCanExecuteChanged();
            ClearSelectionCommand.NotifyCanExecuteChanged();
            ToggleStartStopCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsSelectionEnabled));
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
                await _tcpClient.SendAsync(command);
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
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri pošiljanju STOP: {ex.Message}");
            }
        }

        private async void OnStatusUpdated(object? sender, PlcStatusEventArgs e)
        {
            var status = e.Status;
            _logger.Inform(1, $"MainPageViewModel.OnStatusUpdated start: State={status.State}, LoadedRecipeId={status.LoadedRecipeId}, Step={status.Step}, ErrorCode={status.ErrorCode}");

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

                if (status.State is "1" or "2" && recipe != null && SelectedRecipe?.Id != recipe.Id)
                {
                    var existing = Recipes.FirstOrDefault(r => r.Id == recipe.Id);
                    if (existing is null)
                    {
                        Recipes.Add(recipe);
                        SelectedRecipe = recipe;
                    }
                    else
                    {
                        SelectedRecipe = existing;
                    }
                }

                foreach (var recipeStep in SelectedRecipeSteps)
                {
                    recipeStep.IsActive = status.Step > 0 && recipeStep.StepNumber == status.Step;
                }

                CurrentStepNumber = status.Step;
                PlcErrorCode = status.ErrorCode;

                IsRecipeLoaded = status.State is "1" or "2" or "3";
                IsRunning = status.State == "2";

                LoadedRecipeId = IsRecipeLoaded ? status.LoadedRecipeId : (int?)null;
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

    }
}