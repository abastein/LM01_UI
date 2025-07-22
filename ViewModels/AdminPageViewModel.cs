using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public partial class AdminPageViewModel : ViewModelBase
    {
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly Action<string> _navigate;

        // POPRAVEK: Ročno implementirana lastnost za odpravo napake
        private ViewModelBase? _currentAdminContent;
        public ViewModelBase? CurrentAdminContent
        {
            get => _currentAdminContent;
            set => SetProperty(ref _currentAdminContent, value);
        }

        public IRelayCommand NavigateBackCommand { get; }

        public AdminPageViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext, Action<string> navigate)
        {
            _logger = logger;
            _dbContext = dbContext;
            _navigate = navigate;

            NavigateBackCommand = new RelayCommand(() => _navigate("Welcome"));
            ShowRecipeList();
        }

        private void ShowRecipeList()
        {
            CurrentAdminContent = new RecipeListViewModel(_dbContext, _logger, EditRecipe, AddNewRecipe);
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