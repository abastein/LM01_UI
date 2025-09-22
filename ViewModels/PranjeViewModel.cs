using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;

namespace LM01_UI.ViewModels
{
    public partial class PranjeViewModel : ViewModelBase, IDisposable
    {

        private const int NormalRecipeId = 998;
        private const int IntensiveRecipeId = 999;

        private readonly ApplicationDbContext _dbContext;
        private readonly PlcTcpClient _plcClient;
        private readonly PlcService _plcService;
        private readonly PlcStatusService _plcStatusService;
        private readonly Logger _logger;

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

            NormalnoPranjeCommand = new AsyncRelayCommand(() => ToggleProgramAsync(NormalRecipeId));
            IntenzivnoPranjeCommand = new AsyncRelayCommand(() => ToggleProgramAsync(IntensiveRecipeId));

            _plcStatusService.StatusUpdated += OnPlcStatusUpdated;
        }

        public IAsyncRelayCommand NormalnoPranjeCommand { get; }
        public IAsyncRelayCommand IntenzivnoPranjeCommand { get; }

        public void Dispose()
        {
            _plcStatusService.StatusUpdated -= OnPlcStatusUpdated;
        }

        private async Task ToggleProgramAsync(int recipeId)
        {
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
                _logger.Inform(1, "Drug program je že zagnan in ga ni mogoče preklopiti.");
                return;
            }

            await StartProgramAsync(recipeId);
        }

        private async Task StartProgramAsync(int recipeId)
        {
            try
            {
                Recipe? recipe = await _dbContext.Recipes
                    .Include(r => r.Steps)
                    .FirstOrDefaultAsync(r => r.Id == recipeId);

                if (recipe == null || !recipe.Steps.Any())
                {
                    _logger.Inform(2, $"Receptura {recipeId} ni na voljo ali nima definiranih korakov.");
                    return;
                }

                string loadCommand = _plcService.BuildLoadCommand(recipe);
                await _plcClient.SendAsync(loadCommand);
                await _plcClient.SendAsync(_plcService.GetStartCommand());

                ActiveProgramId = recipeId;
                IsRunning = true;
                UpdateUiForActiveProgram(recipeId);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri zagonu recepture {recipeId}: {ex.Message}");
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

        private void UpdateUiForActiveProgram(int recipeId)
        {
            if (recipeId == NormalRecipeId)
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
            if (recipeId == NormalRecipeId)
            {
                IsNormalnoButtonEnabled = true;
            }
            else
            {
                IsIntenzivnoButtonEnabled = true;
            }
        }

        private void ResetUiState()
        {
            ActiveProgramId = null;
            IsRunning = false;
            NormalnoButtonText = "Normalno pranje";
            NormalnoButtonBrush = Brushes.MediumSeaGreen;
            IntenzivnoButtonText = "Intenzivno pranje";
            IntenzivnoButtonBrush = Brushes.MediumSeaGreen;
            IsOtherButtonEnabled = true;
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
            if (ActiveProgramId is null)
            {
                IsNormalnoButtonEnabled = value;
                IsIntenzivnoButtonEnabled = value;
            }
            else if (ActiveProgramId == NormalRecipeId)
            {
                IsIntenzivnoButtonEnabled = value;
            }
            else if (ActiveProgramId == IntensiveRecipeId)
            {
                IsNormalnoButtonEnabled = value;
            }
        }
    }
}
