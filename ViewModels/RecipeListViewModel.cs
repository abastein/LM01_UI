using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public partial class RecipeListViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;
        private readonly Action<Recipe> _onEditRecipe;
        private readonly Action _onAddNewRecipe;

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteRecipeCommand))]
        private Recipe? _selectedRecipe;

        public RecipeListViewModel(ApplicationDbContext dbContext, Logger logger, Action<Recipe> onEditRecipe, Action onAddNewRecipe)
        {
            _dbContext = dbContext;
            _logger = logger;
            _onEditRecipe = onEditRecipe;
            _onAddNewRecipe = onAddNewRecipe;

            Recipes = new ObservableCollection<Recipe>(_dbContext.Recipes.ToList());

            AddNewRecipeCommand = new RelayCommand(_onAddNewRecipe);
            EditRecipeCommand = new RelayCommand(EditRecipe, CanEditOrDelete);
            DeleteRecipeCommand = new RelayCommand(DeleteRecipe, CanEditOrDelete);
        }

        public IRelayCommand AddNewRecipeCommand { get; }
        public IRelayCommand EditRecipeCommand { get; }
        public IRelayCommand DeleteRecipeCommand { get; }

        private bool CanEditOrDelete() => SelectedRecipe != null;

        private void EditRecipe()
        {
            if (SelectedRecipe != null)
            {
                _onEditRecipe(SelectedRecipe);
            }
        }

        private void DeleteRecipe()
        {
            if (SelectedRecipe != null)
            {
                var recipeToDelete = _dbContext.Recipes.Include(r => r.Steps).FirstOrDefault(r => r.Id == SelectedRecipe.Id);
                if (recipeToDelete != null)
                {
                    _dbContext.Recipes.Remove(recipeToDelete);
                    _dbContext.SaveChanges();
                    Recipes.Remove(SelectedRecipe);
                    _logger.Inform(1, $"Receptura '{SelectedRecipe.Name}' izbrisana.");
                }
            }
        }
    }
}