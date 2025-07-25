using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using LM01_UI; // POPRAVEK: Dodana using direktiva za dostop do PlcTcpClient in Logger

namespace LM01_UI.ViewModels
{
    public partial class AdminPageViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly Action<string> _navigate;
        private readonly PlcTestViewModel _plcTestViewModel;
        private readonly RecipeListViewModel _recipeListViewModel;

        [ObservableProperty]
        private ViewModelBase? _currentAdminContent;

        public AdminPageViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext, Action<string> navigate, PlcTestViewModel plcTestViewModel)
        {
            _logger = logger;
            _dbContext = dbContext;
            _navigate = navigate;
            _plcTestViewModel = plcTestViewModel;

            _recipeListViewModel = new RecipeListViewModel(_dbContext, _logger, EditRecipe, AddNewRecipe);

            NavigateBackCommand = new RelayCommand(() => _navigate("Welcome"));
            NavigateToRecipeListCommand = new RelayCommand(ShowRecipeList);
            NavigateToPlcTestCommand = new RelayCommand(ShowPlcTest);

            ShowRecipeList();
        }

        public IRelayCommand NavigateBackCommand { get; }
        public IRelayCommand NavigateToRecipeListCommand { get; }
        public IRelayCommand NavigateToPlcTestCommand { get; }

        private void ShowRecipeList()
        {
            CurrentAdminContent = _recipeListViewModel;
        }

        private void ShowPlcTest()
        {
            CurrentAdminContent = _plcTestViewModel;
        }

        private void EditRecipe(Recipe recipe)
        {
            var trackedRecipe = _dbContext.Recipes
                                          .Include(r => r.Steps)
                                          .FirstOrDefault(r => r.Id == recipe.Id);
            if (trackedRecipe != null)
            {
                CurrentAdminContent = new RecipeEditorViewModel(trackedRecipe, _dbContext, _logger, ShowRecipeList);
            }
        }

        private void AddNewRecipe()
        {
            var newRecipe = new Recipe { Name = "Nova Receptura" };
            CurrentAdminContent = new RecipeEditorViewModel(newRecipe, _dbContext, _logger, ShowRecipeList);
        }
    }
}