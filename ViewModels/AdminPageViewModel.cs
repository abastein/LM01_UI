using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using LM01_UI; // Za PlcTcpClient in Logger
using LM01_UI.ViewModels; // Za dostop do drugih ViewModelov
using LM01_UI.Data.Persistence; // Za ApplicationDbContext

namespace LM01_UI.ViewModels
{
    public class AdminPageViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly Action<object>? _navigateAction; // Akcija za navigacijo iz MainWindowViewModel

        // ViewModeli za podstrani znotraj Admin strani
        private readonly RecipeListViewModel _recipeListViewModel; // NOVO: Za seznam receptur
        private readonly PlcTestViewModel _plcTestViewModel;
        private readonly ParameterEditorViewModel _parameterEditorViewModel;

        private object? _currentAdminSubPageViewModel;

        public AdminPageViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext, Action<object> navigateAction)
        {
            _plcClient = plcClient;
            _logger = logger;
            _dbContext = dbContext;
            _navigateAction = navigateAction;

            _logger.Inform(1, "AdminPageViewModel initialised.");

            // Inicializacija podrejenih ViewModelov
            _recipeListViewModel = new RecipeListViewModel(_dbContext, _logger, _navigateAction); // NOVO: Injiciramo dbContext in logger
            _plcTestViewModel = new PlcTestViewModel(_plcClient, _logger);
            _parameterEditorViewModel = new ParameterEditorViewModel(_plcClient, _logger, _dbContext);

            // Nastavi privzeto podstran ob vstopu na Admin Page na seznam receptur
            CurrentAdminSubPageViewModel = _recipeListViewModel;

            // Inicializiraj ukaze za navigacijo znotraj Admin Page
            NavigateToRecipeListCommand = new RelayCommand(() => CurrentAdminSubPageViewModel = _recipeListViewModel); // NOVO: Gumb za seznam
            NavigateToPlcTestCommand = new RelayCommand(() => CurrentAdminSubPageViewModel = _plcTestViewModel);
            NavigateToParameterEditorCommand = new RelayCommand(() => CurrentAdminSubPageViewModel = _parameterEditorViewModel);
            NavigateBackCommand = new RelayCommand(NavigateBackToWelcome);
        }

        public object? CurrentAdminSubPageViewModel
        {
            get => _currentAdminSubPageViewModel;
            set => SetProperty(ref _currentAdminSubPageViewModel, value);
        }

        // Commands za navigacijo znotraj Admin strani
        public IRelayCommand NavigateToRecipeListCommand { get; } // NOVO
        public IRelayCommand NavigateToPlcTestCommand { get; }
        public IRelayCommand NavigateToParameterEditorCommand { get; }
        public IRelayCommand NavigateBackCommand { get; }

        private void NavigateBackToWelcome()
        {
            _navigateAction?.Invoke("Welcome");
        }
    }
}