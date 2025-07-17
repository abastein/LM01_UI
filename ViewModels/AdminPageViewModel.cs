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
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly Action<string> _navigate;

        [ObservableProperty]
        private ViewModelBase? _currentAdminContent;

        public AdminPageViewModel(PlcTcpClient plcClient, Logger logger, ApplicationDbContext dbContext, Action<string> navigate)
        {
            _plcClient = plcClient;
            _logger = logger;
            _dbContext = dbContext;
            _navigate = navigate;

            NavigateToRecipeListCommand = new RelayCommand(ShowRecipeList);
            NavigateBackCommand = new RelayCommand(() => _navigate("Welcome"));
            // Inicialno prikažemo seznam receptur
            ShowRecipeList();
        }

        public IRelayCommand NavigateToRecipeListCommand { get; }
        public IRelayCommand NavigateBackCommand { get; }
        // TODO: Dodajte ukaze za ostale poglede, če jih potrebujete
        // public IRelayCommand NavigateToPlcTestCommand { get; } 

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
                CurrentAdminContent = new RecipeEditorViewModel(trackedRecipe, _dbContext, _logger, (s) => ShowRecipeList());
            }
        }

        private void AddNewRecipe()
        {
            var newRecipe = new Recipe { Name = "Nova Receptura" };
            // Ne dodamo je v DbContext takoj, to bo naredil urejevalnik ob shranjevanju
            CurrentAdminContent = new RecipeEditorViewModel(newRecipe, _dbContext, _logger, (s) => ShowRecipeList());
        }
    }
}