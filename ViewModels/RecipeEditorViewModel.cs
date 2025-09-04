using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using LM01_UI.Services;
using LM01_UI.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class RecipeEditorViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;
        private readonly Action _closeAction;

        [ObservableProperty]
        private Recipe _currentRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _steps;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditStepCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStepCommand))]
        private RecipeStep? _selectedStep;

        [ObservableProperty]
        private StepEditorViewModel? _currentStepEditor;

        public RecipeEditorViewModel(Recipe recipe, ApplicationDbContext dbContext, Logger logger, Action closeAction)
        {
            _dbContext = dbContext;
            _logger = logger;
            _closeAction = closeAction;
            _currentRecipe = recipe;

            Steps = new ObservableCollection<RecipeStep>(_currentRecipe.Steps.OrderBy(s => s.StepNumber));

            SaveRecipeCommand = new AsyncRelayCommand(SaveRecipeAsync);
            CancelCommand = new RelayCommand(_closeAction);
            AddStepCommand = new AsyncRelayCommand(AddStepAsync);
            EditStepCommand = new AsyncRelayCommand(EditStepAsync, () => SelectedStep != null);
            DeleteStepCommand = new AsyncRelayCommand(DeleteStepAsync, () => SelectedStep != null);
        }

        public IAsyncRelayCommand SaveRecipeCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IAsyncRelayCommand AddStepCommand { get; }
        public IAsyncRelayCommand EditStepCommand { get; }
        public IAsyncRelayCommand DeleteStepCommand { get; }

        private async Task SaveRecipeAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentRecipe.Name))
            {
                await MessageBoxManager.GetMessageBoxStandard("Napaka", "Ime recepture ne sme biti prazno.").ShowAsync();
                return;
            }
            try
            {
                RenumberSteps();
                CurrentRecipe.Steps = new ObservableCollection<RecipeStep>(Steps);

                if (CurrentRecipe.Id == 0) { _dbContext.Recipes.Add(CurrentRecipe); }

                await _dbContext.SaveChangesAsync();
                _logger.Inform(1, $"Receptura '{CurrentRecipe.Name}' uspešno shranjena.");
                _closeAction();
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri shranjevanju recepture: {ex.Message}"); }
        }

        private async Task AddStepAsync()
        {
            RecipeStep? newStepResult = null;
            var editorWindow = new Window { Title = "Nov Korak", SizeToContent = SizeToContent.WidthAndHeight, SystemDecorations = SystemDecorations.BorderOnly, WindowStartupLocation = WindowStartupLocation.CenterOwner };

            Action<RecipeStep?> closeCallback = (stepResult) =>
            {
                newStepResult = stepResult;
                editorWindow.Close();
            };

            var nextStepNumber = Steps.Any() ? Steps.Max(s => s.StepNumber) + 1 : 1;
            var newStepObject = new RecipeStep { StepNumber = nextStepNumber };
            var editorViewModel = new StepEditorViewModel(newStepObject, closeCallback);
            editorWindow.Content = new StepEditorView { DataContext = editorViewModel };

            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var parent = lifetime?.Windows.FirstOrDefault(w => w.DataContext == this) ?? lifetime?.MainWindow;

            if (parent is not null)
                await editorWindow.ShowDialog(parent);

            if (newStepResult != null)
            {
                Steps.Add(newStepResult);
                RenumberSteps();
                await Dispatcher.UIThread.InvokeAsync(() =>
                    Steps = new ObservableCollection<RecipeStep>(Steps.OrderBy(s => s.StepNumber)));
            }
        }

        private async Task EditStepAsync()
        {
            if (SelectedStep == null) return;

            RecipeStep? editedStepResult = null;
            var stepToEdit = SelectedStep;
            var editorWindow = new Window { Title = $"Urejanje koraka {stepToEdit.StepNumber}", SizeToContent = SizeToContent.WidthAndHeight, SystemDecorations = SystemDecorations.BorderOnly, WindowStartupLocation = WindowStartupLocation.CenterOwner };

            Action<RecipeStep?> closeCallback = (stepResult) =>
            {
                editedStepResult = stepResult;
                editorWindow.Close();
            };

            var editorViewModel = new StepEditorViewModel(stepToEdit, closeCallback);
            editorWindow.Content = new StepEditorView { DataContext = editorViewModel };

            var parent = (Avalonia.Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?
                .Windows.FirstOrDefault(w => w.DataContext == this);
            await editorWindow.ShowDialog(parent);

            if (editedStepResult != null)
            {
                var index = Steps.IndexOf(stepToEdit);
                if (index != -1) { Steps[index] = editedStepResult; }
                RenumberSteps();
                await Dispatcher.UIThread.InvokeAsync(() =>
                    Steps = new ObservableCollection<RecipeStep>(Steps.OrderBy(s => s.StepNumber)));
            }
        }

        private async Task DeleteStepAsync()
        {
            if (SelectedStep == null) return;
            var box = MessageBoxManager.GetMessageBoxStandard("Potrdi brisanje", $"Ali ste prepričani, da želite izbrisati korak št. {SelectedStep.StepNumber}?", ButtonEnum.YesNo, Icon.Warning);
            var result = await box.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                Steps.Remove(SelectedStep);
                RenumberSteps();
            }
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].StepNumber = i + 1;
            }
        }
    }
}