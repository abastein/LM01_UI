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
    public partial class RecipeEditorViewModel : ViewModelBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Logger _logger;
        private readonly Action<string> _navigateBack;

        [ObservableProperty]
        private Recipe _currentRecipe;

        [ObservableProperty]
        private ObservableCollection<RecipeStep> _steps;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditStepCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteStepCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveStepUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveStepDownCommand))]
        private RecipeStep? _selectedStep;

        [ObservableProperty]
        private ViewModelBase? _currentStepEditor;

        public RecipeEditorViewModel(Recipe recipe, ApplicationDbContext dbContext, Logger logger, Action<string> navigateBack)
        {
            _currentRecipe = recipe;
            _dbContext = dbContext;
            _logger = logger;
            _navigateBack = navigateBack;

            Steps = new ObservableCollection<RecipeStep>(
                _currentRecipe.Steps.OrderBy(s => s.StepNumber)
            );

            SaveRecipeCommand = new RelayCommand(SaveChanges);
            AddNewStepCommand = new RelayCommand(AddNewStep);
            EditStepCommand = new RelayCommand(EditStep, CanEditOrDeleteStep);
            DeleteStepCommand = new RelayCommand(DeleteStep, CanEditOrDeleteStep);
            CloseEditorCommand = new RelayCommand(CloseStepEditor);
            MoveStepUpCommand = new RelayCommand(MoveStepUp, CanMoveStepUp);
            MoveStepDownCommand = new RelayCommand(MoveStepDown, CanMoveStepDown);
            CancelCommand = new RelayCommand(() => _navigateBack("Admin")); // Ukaz za nazaj
        }

        public IRelayCommand SaveRecipeCommand { get; }
        public IRelayCommand AddNewStepCommand { get; }
        public IRelayCommand EditStepCommand { get; }
        public IRelayCommand DeleteStepCommand { get; }
        public IRelayCommand CloseEditorCommand { get; }
        public IRelayCommand MoveStepUpCommand { get; }
        public IRelayCommand MoveStepDownCommand { get; }
        public IRelayCommand CancelCommand { get; } // Nov ukaz

        private bool CanEditOrDeleteStep() => SelectedStep != null;
        private bool CanMoveStepUp() => SelectedStep != null && Steps.FirstOrDefault() != SelectedStep;
        private bool CanMoveStepDown() => SelectedStep != null && Steps.LastOrDefault() != SelectedStep;

        private void SaveChanges()
        {
            // Če je recept nov (nima ID-ja), ga dodamo v DbContext pred shranjevanjem
            if (CurrentRecipe.Id == 0)
            {
                _dbContext.Recipes.Add(CurrentRecipe);
            }

            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].StepNumber = i + 1;
            }

            CurrentRecipe.Steps = Steps.ToList();
            _dbContext.SaveChanges();
            _logger.Inform(1, $"Receptura '{CurrentRecipe.Name}' shranjena.");
            _navigateBack("Admin");
        }

        private void CloseStepEditor()
        {
            CurrentStepEditor = null;
        }

        private void EditStep()
        {
            if (SelectedStep != null)
            {
                CurrentStepEditor = new StepEditorViewModel(SelectedStep);
            }
        }

        private void AddNewStep()
        {
            var newStep = new RecipeStep
            {
                RecipeId = CurrentRecipe.Id,
                StepNumber = Steps.Count + 1
            };

            Steps.Add(newStep);
            SelectedStep = newStep;
            CurrentStepEditor = new StepEditorViewModel(newStep);
        }

        private void DeleteStep()
        {
            if (SelectedStep != null)
            {
                // Pomembno: če korak še ni bil shranjen v bazo, ga samo odstranimo iz kolekcije
                if (SelectedStep.Id != 0)
                {
                    _dbContext.Steps.Remove(SelectedStep);
                }
                Steps.Remove(SelectedStep);
                CloseStepEditor();
            }
        }

        private void MoveStepUp()
        {
            if (SelectedStep == null) return;
            int oldIndex = Steps.IndexOf(SelectedStep);
            if (oldIndex > 0)
            {
                Steps.Move(oldIndex, oldIndex - 1);
            }
        }

        private void MoveStepDown()
        {
            if (SelectedStep == null) return;
            int oldIndex = Steps.IndexOf(SelectedStep);
            if (oldIndex < Steps.Count - 1)
            {
                Steps.Move(oldIndex, oldIndex + 1);
            }
        }
    }
}