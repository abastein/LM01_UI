using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media; // <<<<<<< DODAJTE TA USING za Brushes in Colors
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public class RecipeListViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;
        private readonly Action<object>? _navigateAction;
        private ObservableCollection<Recipe> _recipes = new();
        private Recipe? _selectedRecipe;

        public RecipeListViewModel(ApplicationDbContext dbContext, Logger logger, Action<object> navigateAction)
        {
            _dbContext = dbContext;
            _logger = logger;
            _navigateAction = navigateAction;
            LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
            AddNewRecipeCommand = new AsyncRelayCommand(AddNewRecipeAsync);
            EditRecipeCommand = new AsyncRelayCommand(EditSelectedRecipe, () => SelectedRecipe != null);
            DeleteRecipeCommand = new AsyncRelayCommand(DeleteSelectedRecipe, () => SelectedRecipe != null);
            _ = LoadRecipesAsync();
        }

        public ObservableCollection<Recipe> Recipes { get => _recipes; private set => SetProperty(ref _recipes, value); }
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetProperty(ref _selectedRecipe, value))
                {
                    ((AsyncRelayCommand)EditRecipeCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteRecipeCommand).NotifyCanExecuteChanged();
                }
            }
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
                Recipes.Clear();
                foreach (var recipe in recipesFromDb) { Recipes.Add(recipe); }
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju receptur: {ex.Message}"); }
        }

        private async Task AddNewRecipeAsync()
        {
            var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            var editorWindow = new Window
            {
                Title = "Nova Receptura",
                SystemDecorations = SystemDecorations.None,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                // POPRAVEK: Nastavimo pol-prosojno črno ozadje za "dimming" efekt
                Background = new SolidColorBrush(Colors.Black, 0.65),
                // Velikost nastavimo na velikost glavnega okna
                Width = mainWindow?.ClientSize.Width ?? 1920,
                Height = mainWindow?.ClientSize.Height ?? 1080,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var editorViewModel = new RecipeEditorViewModel(_dbContext, _logger, () => editorWindow.Close());
            editorWindow.Content = new Views.RecipeEditorView { DataContext = editorViewModel };

            await editorWindow.ShowDialog(mainWindow!);
            await LoadRecipesAsync();
        }

        private async Task EditSelectedRecipe()
        {
            if (SelectedRecipe == null) return;
            var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            var editorWindow = new Window
            {
                Title = $"Urejanje: {SelectedRecipe.Name}",
                SystemDecorations = SystemDecorations.None,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                // POPRAVEK: Enaka nastavitev tudi tukaj
                Background = new SolidColorBrush(Colors.Black, 0.65),
                Width = mainWindow?.ClientSize.Width ?? 1920,
                Height = mainWindow?.ClientSize.Height ?? 1080,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var editorViewModel = new RecipeEditorViewModel(SelectedRecipe, _dbContext, _logger, () => editorWindow.Close());
            editorWindow.Content = new Views.RecipeEditorView { DataContext = editorViewModel };

            await editorWindow.ShowDialog(mainWindow!);
            await LoadRecipesAsync();
        }

        private async Task DeleteSelectedRecipe()
        {
            if (SelectedRecipe == null) return;
            var result = await MessageBoxManager.GetMessageBoxStandard("Potrdi brisanje", $"Ali ste prepričani, da želite izbrisati recepturo '{SelectedRecipe.Name}'?", ButtonEnum.YesNo, Icon.Warning).ShowAsync();
            if (result == ButtonResult.Yes)
            {
                try
                {
                    _dbContext.Recipes.Remove(SelectedRecipe);
                    await _dbContext.SaveChangesAsync();
                    Recipes.Remove(SelectedRecipe);
                }
                catch (Exception ex) { _logger.Inform(2, $"Napaka pri brisanju recepture: {ex.Message}"); }
            }
        }
    }
}