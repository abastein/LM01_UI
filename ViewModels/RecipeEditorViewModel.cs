using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Data.Persistence;
using LM01_UI.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public class RecipeEditorViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;
        private Action? _closeAction;
        private RecipeStep? _selectedStep;

        public Recipe CurrentRecipe { get; set; }
        public ObservableCollection<RecipeStep> CurrentRecipeSteps { get; set; } = new();
        public RecipeStep? SelectedStep { get => _selectedStep; set => SetProperty(ref _selectedStep, value); }

        public RecipeEditorViewModel(ApplicationDbContext dbContext, Logger logger, Action closeAction)
        {
            _dbContext = dbContext;
            _logger = logger;
            _closeAction = closeAction;
            CurrentRecipe = new Recipe();
            InitializeCommands();
        }

        public RecipeEditorViewModel(Recipe recipeToEdit, ApplicationDbContext dbContext, Logger logger, Action closeAction)
        {
            _dbContext = dbContext;
            _logger = logger;
            _closeAction = closeAction;
            CurrentRecipe = recipeToEdit;
            _ = LoadRecipeStepsAsync();
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            SaveRecipeCommand = new AsyncRelayCommand(SaveRecipeAsync);
            CancelCommand = new RelayCommand(Cancel);
            AddStepCommand = new AsyncRelayCommand(AddStepAsync);
            EditStepCommand = new AsyncRelayCommand(EditStepAsync, () => SelectedStep != null);
            DeleteStepCommand = new AsyncRelayCommand(DeleteStepAsync, () => SelectedStep != null);
            this.WhenAnyValue(x => x.SelectedStep).Subscribe(_ => { ((AsyncRelayCommand)EditStepCommand).NotifyCanExecuteChanged(); ((AsyncRelayCommand)DeleteStepCommand).NotifyCanExecuteChanged(); });
        }

        public void SetCloseAction(Action action) => _closeAction = action;

        public IAsyncRelayCommand SaveRecipeCommand { get; private set; } = null!;
        public IRelayCommand CancelCommand { get; private set; } = null!;
        public IAsyncRelayCommand AddStepCommand { get; private set; } = null!;
        public IAsyncRelayCommand EditStepCommand { get; private set; } = null!;
        public IAsyncRelayCommand DeleteStepCommand { get; private set; } = null!;

        private async Task LoadRecipeStepsAsync()
        {
            try
            {
                var stepsFromDb = await _dbContext.RecipeSteps.Where(s => s.RecipeId == CurrentRecipe.Id).OrderBy(s => s.StepNumber).ToListAsync();
                CurrentRecipeSteps.Clear();
                foreach (var step in stepsFromDb) { CurrentRecipeSteps.Add(step); }
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri nalaganju korakov recepture: {ex.Message}"); }
        }

        private async Task SaveRecipeAsync()
        {
            RenumberSteps();
            if (string.IsNullOrWhiteSpace(CurrentRecipe.Name))
            {
                await MessageBoxManager.GetMessageBoxStandard("Napaka", "Ime recepture ne sme biti prazno.", ButtonEnum.Ok, Icon.Error).ShowAsync();
                return;
            }
            try
            {
                CurrentRecipe.Steps.Clear();
                foreach (var uiStep in CurrentRecipeSteps)
                {
                    CurrentRecipe.Steps.Add(new RecipeStep { StepNumber = uiStep.StepNumber, Function = uiStep.Function, SpeedRPM = uiStep.SpeedRPM, Direction = uiStep.Direction, TargetXDeg = uiStep.TargetXDeg, Repeats = uiStep.Repeats, PauseMs = uiStep.PauseMs });
                }

                if (CurrentRecipe.Id == 0) { _dbContext.Recipes.Add(CurrentRecipe); }
                else { _dbContext.Recipes.Update(CurrentRecipe); }

                await _dbContext.SaveChangesAsync();
                _closeAction?.Invoke();
            }
            catch (Exception ex) { _logger.Inform(2, $"Napaka pri shranjevanju recepture: {ex.Message}"); }
        }

        private void Cancel() => _closeAction?.Invoke();

        private async Task AddStepAsync()
        {
            RecipeStep? newStepResult = null;
            var editorWindow = new Window { Title = "Nov Korak", SizeToContent = SizeToContent.WidthAndHeight, SystemDecorations = SystemDecorations.BorderOnly, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            Action<RecipeStep?> closeCallback = (stepResult) => { newStepResult = stepResult; editorWindow.Close(); };
            var nextStepNumber = CurrentRecipeSteps.Any() ? CurrentRecipeSteps.Max(s => s.StepNumber) + 1 : 1;
            var editorViewModel = new StepEditorViewModel(nextStepNumber, closeCallback);
            editorWindow.Content = new Views.StepEditorView { DataContext = editorViewModel };
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                await editorWindow.ShowDialog(desktop.MainWindow);
            }
            if (newStepResult != null) { CurrentRecipeSteps.Add(newStepResult); RenumberSteps(); }
        }

        private async Task EditStepAsync()
        {
            if (SelectedStep == null) return;
            var originalStep = SelectedStep;
            RecipeStep? editedStepResult = null;
            var editorWindow = new Window { Title = $"Urejanje koraka {SelectedStep.StepNumber}", SizeToContent = SizeToContent.WidthAndHeight, SystemDecorations = SystemDecorations.BorderOnly, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            Action<RecipeStep?> closeCallback = (stepResult) => { editedStepResult = stepResult; editorWindow.Close(); };
            var editorViewModel = new StepEditorViewModel(originalStep, closeCallback);
            editorWindow.Content = new Views.StepEditorView { DataContext = editorViewModel };
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                await editorWindow.ShowDialog(desktop.MainWindow);
            }
            if (editedStepResult != null) { var index = CurrentRecipeSteps.IndexOf(originalStep); if (index != -1) { CurrentRecipeSteps[index] = editedStepResult; } RenumberSteps(); }
        }

        private async Task DeleteStepAsync()
        {
            if (SelectedStep == null) return;
            var result = await MessageBoxManager.GetMessageBoxStandard("Potrdi brisanje koraka", $"Ali ste prepričani, da želite izbrisati korak {SelectedStep.StepNumber}?", ButtonEnum.YesNo, Icon.Warning).ShowAsync();
            if (result == ButtonResult.Yes) { CurrentRecipeSteps.Remove(SelectedStep); RenumberSteps(); }
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < CurrentRecipeSteps.Count; i++) { if (CurrentRecipeSteps[i].StepNumber != i + 1) { CurrentRecipeSteps[i].StepNumber = i + 1; } }
        }
    }
}