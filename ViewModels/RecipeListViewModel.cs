using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using LM01_UI.Views;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class RecipeListViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditRecipeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteRecipeCommand))]
        private Recipe? _selectedRecipe;

        public RecipeListViewModel(ApplicationDbContext dbContext, Logger logger, Action<Recipe> onEditRecipe, Action onAddNewRecipe)
        {
            _dbContext = dbContext;
            _logger = logger;

            AddNewRecipeCommand = new AsyncRelayCommand(AddNewRecipeAsync);
            EditRecipeCommand = new AsyncRelayCommand(EditSelectedRecipeAsync, () => SelectedRecipe != null);
            DeleteRecipeCommand = new AsyncRelayCommand(DeleteSelectedRecipeAsync, () => SelectedRecipe != null);

            _ = LoadRecipesAsync();
        }

        public IAsyncRelayCommand AddNewRecipeCommand { get; }
        public IAsyncRelayCommand EditRecipeCommand { get; }
        public IAsyncRelayCommand DeleteRecipeCommand { get; }

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipesFromDb = await _dbContext.Recipes
                .OrderBy(r => r.Id)
                .ToListAsync();
                Recipes = new ObservableCollection<Recipe>(recipesFromDb);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}"); }
        }

        private async Task AddNewRecipeAsync()
        {
            var newRecipe = new Recipe { Name = "" };
            await OpenRecipeEditor(newRecipe);
        }

        private async Task EditSelectedRecipeAsync()
        {
            if (SelectedRecipe == null) return;
            var recipeToEdit = await _dbContext.Recipes
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == SelectedRecipe.Id);

            if (recipeToEdit != null)
            {
                await OpenRecipeEditor(recipeToEdit);
            }
        }

        private async Task OpenRecipeEditor(Recipe recipe)
        {
            var editorWindow = new Window
            {
                Title = "Urejevalnik Recepture",
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            var editorViewModel = new RecipeEditorViewModel(
                recipe, _dbContext, _logger, () => editorWindow.Close());

            editorWindow.DataContext = editorViewModel;      // <-- new
            editorWindow.Content = new RecipeEditorView(); // DataContext already set on window

            var mainWindow = (App.Current as App)?.GetMainWindow();
            if (mainWindow != null)
                await editorWindow.ShowDialog(mainWindow);
            else
                editorWindow.Show();

            await LoadRecipesAsync();
        }


        private async Task DeleteSelectedRecipeAsync()
        {
            if (SelectedRecipe == null) return;
            var box = MessageBoxManager.GetMessageBoxStandard("Potrdi brisanje", $"Ali ste prepričani, da želite izbrisati recepturo '{SelectedRecipe.Name}'?", ButtonEnum.YesNo, Icon.Warning);
            var result = await box.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                try
                {
                    _dbContext.Recipes.Remove(SelectedRecipe);
                    await _dbContext.SaveChangesAsync();
                    Recipes.Remove(SelectedRecipe);
                    _logger.Inform(1, $"Receptura '{SelectedRecipe.Name}' uspešno izbrisana.");
                }
                catch (Exception ex) { _logger.Inform(2, $"Napaka pri brisanju recepture: {ex.Message}"); }
            }
        }
    }
}