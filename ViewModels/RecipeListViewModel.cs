using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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

        public RecipeListViewModel(ApplicationDbContext dbContext, Logger logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
            AddNewRecipeCommand = new AsyncRelayCommand(AddNewRecipeAsync);
            EditRecipeCommand = new AsyncRelayCommand(EditSelectedRecipeAsync, () => SelectedRecipe != null);
            DeleteRecipeCommand = new AsyncRelayCommand(DeleteSelectedRecipeAsync, () => SelectedRecipe != null);

            // Naložimo recepte ob zagonu
            _ = LoadRecipesAsync();
        }

        public IAsyncRelayCommand LoadRecipesCommand { get; }
        public IAsyncRelayCommand AddNewRecipeCommand { get; }
        public IAsyncRelayCommand EditRecipeCommand { get; }
        public IAsyncRelayCommand DeleteRecipeCommand { get; }

        private async Task LoadRecipesAsync()
        {
            try
            {
                var recipesFromDb = await _dbContext.Recipes.OrderBy(r => r.Name).ToListAsync();
                Recipes = new ObservableCollection<Recipe>(recipesFromDb);
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}"); }
        }

        private async Task AddNewRecipeAsync()
        {
            var newRecipe = new Recipe { Name = "Nova Receptura" };
            await OpenRecipeEditor(newRecipe);
        }

        private async Task EditSelectedRecipeAsync()
        {
            if (SelectedRecipe == null) return;
            // Naložimo celoten recept s koraki iz baze
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
            var editorWindow = new Window { Title = "Urejevalnik Recepture", WindowStartupLocation = WindowStartupLocation.CenterScreen, SizeToContent = SizeToContent.WidthAndHeight };

            // Ustvarimo nov ViewModel za urejevalnik in mu podamo recept ter akcijo za zapiranje
            var editorViewModel = new RecipeEditorViewModel(recipe, _dbContext, _logger, () => editorWindow.Close());
            editorWindow.Content = new RecipeEditorView { DataContext = editorViewModel };

            // Pokažemo okno kot dialog
            await editorWindow.ShowDialog((App.Current as App)!.GetMainWindow());

            // Ko se dialog zapre, osvežimo seznam receptur
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