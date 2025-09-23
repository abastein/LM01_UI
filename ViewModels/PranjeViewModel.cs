using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Enums;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;

namespace LM01_UI.ViewModels
{
    public partial class PranjeViewModel : ViewModelBase, IDisposable
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly PlcService _plcService;
        private readonly PlcStatusService _plcStatusService;
        private readonly Logger _logger;

        private readonly SemaphoreSlim _systemRecipeLookupLock = new(1, 1);
        private int? _normalRecipeId;
        private int? _intensiveRecipeId;
        private RecipeSystemKey? _activeProgramKey;


        [ObservableProperty]
        private int? _activeProgramId;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _normalnoButtonText = "Normalno pranje";

        [ObservableProperty]
        private IBrush _normalnoButtonBrush = Brushes.MediumSeaGreen;

        [ObservableProperty]
        private string _intenzivnoButtonText = "Intenzivno pranje";

        [ObservableProperty]
        private IBrush _intenzivnoButtonBrush = Brushes.MediumSeaGreen;

        [ObservableProperty]
        private bool _isOtherButtonEnabled = true;

        [ObservableProperty]
        private bool _isNormalnoButtonEnabled = true;

        [ObservableProperty]
        private bool _isIntenzivnoButtonEnabled = true;

        public PranjeViewModel(
            ApplicationDbContext dbContext,
            PlcTcpClient plcClient,
            PlcService plcService,
            PlcStatusService plcStatusService,
            Logger logger)
        {
            _dbContext = dbContext;
            _plcClient = plcClient;
            _plcService = plcService;
            _plcStatusService = plcStatusService;
            _logger = logger;

            NormalnoPranjeCommand = new AsyncRelayCommand(() => ToggleProgramAsync(RecipeSystemKey.NormalWash));
            IntenzivnoPranjeCommand = new AsyncRelayCommand(() => ToggleProgramAsync(RecipeSystemKey.IntensiveWash));

            _plcStatusService.StatusUpdated += OnPlcStatusUpdated;
        }

        public IAsyncRelayCommand NormalnoPranjeCommand { get; }
        public IAsyncRelayCommand IntenzivnoPranjeCommand { get; }

        public void Dispose()
        {
            _plcStatusService.StatusUpdated -= OnPlcStatusUpdated;
        }

        private async Task ToggleProgramAsync(RecipeSystemKey recipeKey)
        {
            string programLabel = GetProgramLabel(recipeKey);

            int? recipeId = await GetRecipeIdAsync(recipeKey);

            if (recipeId is null)
            {
                _logger.Inform(2, $"Sistemska receptura '{programLabel}' ni na voljo.");
                return;
            }

            if (!_plcClient.IsConnected)
            {
                _logger.Inform(2, "PLC ni povezan – pranja ni mogoče zagnati.");
                return;
            }

            if (ActiveProgramId == recipeId && IsRunning)
            {
                await StopProgramAsync();
                return;
            }

            if (IsRunning)
            {
                string activeLabel = _activeProgramKey is RecipeSystemKey key
                    ? GetProgramLabel(key)
                : ActiveProgramId?.ToString() ?? "?";
                _logger.Inform(1, $"Program '{activeLabel}' je že zagnan in ga ni mogoče preklopiti.");
                return;
            }

            await StartProgramAsync(recipeId.Value, recipeKey);
        }

        private async Task StartProgramAsync(int recipeId, RecipeSystemKey recipeKey)
        {
            string programLabel = GetProgramLabel(recipeKey);
            try
            {
                Recipe? recipe = await _dbContext.Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == recipeId);

                if (recipe == null || !recipe.Steps.Any())
                {
                    _logger.Inform(2, $"Receptura '{programLabel}' (ID: {recipeId}) ni na voljo ali nima definiranih korakov.");
                    return;
                }

                string loadCommand = _plcService.BuildLoadCommand(recipe);
                await _plcClient.SendAsync(loadCommand);
                await _plcClient.SendAsync(_plcService.GetStartCommand());

                ActiveProgramId = recipeId;
                IsRunning = true;
                UpdateUiForActiveProgram(recipeKey);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri zagonu recepture '{programLabel}' (ID: {recipeId}): {ex.Message}");
            }
        }

        private async Task StopProgramAsync()

        {
            try
            {
                await _plcClient.SendAsync(_plcService.GetStopCommand());
                await _plcClient.SendAsync(_plcService.GetUnloadCommand());

                ResetUiState();
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri zaustavitvi programa: {ex.Message}");
            }

        }

        private void UpdateUiForActiveProgram(RecipeSystemKey recipeKey)
        {
            _activeProgramKey = recipeKey;

            if (recipeKey == RecipeSystemKey.NormalWash)
            {
                NormalnoButtonText = "Ustavi normalno pranje";
                NormalnoButtonBrush = Brushes.IndianRed;
                IntenzivnoButtonText = "Intenzivno pranje";
                IntenzivnoButtonBrush = Brushes.MediumSeaGreen;
            }
            else
            {
                IntenzivnoButtonText = "Ustavi intenzivno pranje";
                IntenzivnoButtonBrush = Brushes.IndianRed;
                NormalnoButtonText = "Normalno pranje";
                NormalnoButtonBrush = Brushes.MediumSeaGreen;
            }

            IsOtherButtonEnabled = false;
            IsNormalnoButtonEnabled = true;
            IsIntenzivnoButtonEnabled = true;
        }

        private void ResetUiState()
        {
            ActiveProgramId = null;
            IsRunning = false;
            _activeProgramKey = null;
            IsNormalnoButtonEnabled = true;
            IsIntenzivnoButtonEnabled = true;
            NormalnoButtonText = "Normalno pranje";
            NormalnoButtonBrush = Brushes.MediumSeaGreen;
            IntenzivnoButtonText = "Intenzivno pranje";
            IntenzivnoButtonBrush = Brushes.MediumSeaGreen;
            IsOtherButtonEnabled = true;
            IsNormalnoButtonEnabled = true;
            IsIntenzivnoButtonEnabled = true;
        }

        private void OnPlcStatusUpdated(object? sender, PlcStatusEventArgs e)
        {
            if (ActiveProgramId is null)
            {
                return;
            }

            var status = e.Status;
            if (status.LoadedRecipeId != ActiveProgramId || status.State == "0" || status.State == "3")
            {
                ResetUiState();
            }
        }

        partial void OnIsOtherButtonEnabledChanged(bool value)
        {
            if (_activeProgramKey is null)
            {
                IsNormalnoButtonEnabled = value;
                IsIntenzivnoButtonEnabled = value;
            }
            else if (_activeProgramKey == RecipeSystemKey.NormalWash)
            {
                IsIntenzivnoButtonEnabled = value;
            }
            else if (_activeProgramKey == RecipeSystemKey.IntensiveWash)
            {
                IsNormalnoButtonEnabled = value;
            }
        }

        private async Task<int?> GetRecipeIdAsync(RecipeSystemKey recipeKey)
        {
            await _systemRecipeLookupLock.WaitAsync();
            try
            {
                if (recipeKey == RecipeSystemKey.NormalWash && _normalRecipeId.HasValue)
                {
                    return _normalRecipeId;
                }

                if (recipeKey == RecipeSystemKey.IntensiveWash && _intensiveRecipeId.HasValue)
                {
                    return _intensiveRecipeId;
                }

                int? recipeId = await _dbContext.Recipes
                    .AsNoTracking()
                    .Where(r => r.SystemKey == recipeKey)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();

                if (recipeKey == RecipeSystemKey.NormalWash && recipeId.HasValue)
                {
                    _normalRecipeId = recipeId;
                }
                else if (recipeKey == RecipeSystemKey.IntensiveWash && recipeId.HasValue)
                {
                    _intensiveRecipeId = recipeId;
                }

                return recipeId;
            }
            finally
            {
                _systemRecipeLookupLock.Release();
            }
        }

        private static string GetProgramLabel(RecipeSystemKey recipeKey) => recipeKey switch
        {
            RecipeSystemKey.NormalWash => "Normalno pranje",
            RecipeSystemKey.IntensiveWash => "Intenzivno pranje",
            _ => recipeKey.ToString()
        };
    }
}
