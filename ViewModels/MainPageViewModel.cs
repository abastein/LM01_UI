using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using Microsoft.EntityFrameworkCore;

namespace LM01_UI.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;

        private ObservableCollection<Recipe> _recipes = new();
        private Recipe? _selectedRecipe;
        private bool _isProcedureRunning;

        public MainPageViewModel(ApplicationDbContext dbContext, PlcTcpClient plcClient, Logger logger)
        {
            _dbContext = dbContext;
            _plcClient = plcClient;
            _logger = logger;

            LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
            LoadRecipeCommand = new AsyncRelayCommand(LoadRecipeOnPlcAsync, () => SelectedRecipe != null && !IsProcedureRunning);
            ToggleStartStopCommand = new RelayCommand(ToggleStartStop);
            ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedRecipe != null);

            _ = LoadRecipesCommand.ExecuteAsync(null);
        }

        public ObservableCollection<Recipe> Recipes
        {
            get => _recipes;
            set => SetProperty(ref _recipes, value);
        }

        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    LoadRecipeCommand.NotifyCanExecuteChanged();
                    ClearSelectionCommand.NotifyCanExecuteChanged();
                    _ = LoadStepsForSelectedRecipeAsync();
                }
            }
        }

        public ObservableCollection<RecipeStep> SelectedRecipeSteps { get; } = new();

        public bool IsProcedureRunning
        {
            get => _isProcedureRunning;
            set
            {
                if (SetProperty(ref _isProcedureRunning, value))
                {
                    OnPropertyChanged(nameof(StartStopButtonText));
                    OnPropertyChanged(nameof(StartStopButtonBrush));
                    LoadRecipeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string StartStopButtonText => IsProcedureRunning ? "Stop" : "Start";
        public IBrush StartStopButtonBrush => IsProcedureRunning ? Brushes.IndianRed : Brushes.MediumSeaGreen;

        public IAsyncRelayCommand LoadRecipesCommand { get; }
        public IAsyncRelayCommand LoadRecipeCommand { get; }
        public IRelayCommand ToggleStartStopCommand { get; }
        public IRelayCommand ClearSelectionCommand { get; }

        private async Task LoadRecipesAsync()
        {
            _logger.Inform(1, "Nalaganje seznama receptur za izvajanje...");
            var recipesFromDb = await _dbContext.Recipes.OrderBy(r => r.Name).ToListAsync();
            Recipes = new ObservableCollection<Recipe>(recipesFromDb);
            _logger.Inform(1, $"Naloženih {Recipes.Count} receptur.");
        }

        private async Task LoadStepsForSelectedRecipeAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SelectedRecipeSteps.Clear();
                if (SelectedRecipe != null)
                {
                    _logger.Inform(1, $"Nalaganje korakov za recepturo '{SelectedRecipe.Name}'...");
                    var steps = await _dbContext.RecipeSteps
                        .Where(s => s.RecipeId == SelectedRecipe.Id)
                        .OrderBy(s => s.StepNumber)
                        .ToListAsync();

                    foreach (var step in steps)
                    {
                        SelectedRecipeSteps.Add(step);
                    }
                    _logger.Inform(1, $"Naloženih {SelectedRecipeSteps.Count} korakov.");
                }
            });
        }

        private void ClearSelection()
        {
            SelectedRecipe = null;
            _logger.Inform(1, "Izbira počiščena.");
        }

        private async Task LoadRecipeOnPlcAsync()
        {
            if (SelectedRecipe == null) return;
            _logger.Inform(1, $"Pošiljanje recepture '{SelectedRecipe.Name}' na PLC...");
            // TODO: Dejanska logika za pošiljanje
            await Task.Delay(500);
            _logger.Inform(1, "Receptura poslana.");
        }

        private void ToggleStartStop()
        {
            IsProcedureRunning = !IsProcedureRunning;

            if (IsProcedureRunning)
            {
                _logger.Inform(1, "Zagon postopka...");
                // TODO: Dejanska logika za pošiljanje START
            }
            else
            {
                _logger.Inform(1, "Ustavitev postopka...");
                // TODO: Dejanska logika za pošiljanje STOP
            }
        }
    }
}