using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Enums;
using LM01_UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public class StepEditorViewModel : ViewModelBase
    {
        private Action<RecipeStep?>? _closeAction;
        public RecipeStep CurrentStep { get; private set; }

        public List<FunctionType> FunctionTypes { get; } = Enum.GetValues(typeof(FunctionType)).Cast<FunctionType>().ToList();
        public List<DirectionType> DirectionTypes { get; } = Enum.GetValues(typeof(DirectionType)).Cast<DirectionType>().ToList();

        // Inicializacija polj za odpravo opozoril
        private string _stepNumberString = "";
        public string StepNumberString { get => _stepNumberString; set => SetProperty(ref _stepNumberString, value); }
        private string _speedRpmString = "";
        public string SpeedRpmString { get => _speedRpmString; set => SetProperty(ref _speedRpmString, value); }
        private string _targetXDegString = "";
        public string TargetXDegString { get => _targetXDegString; set => SetProperty(ref _targetXDegString, value); }
        private string _repeatsString = "";
        public string RepeatsString { get => _repeatsString; set => SetProperty(ref _repeatsString, value); }
        private string _pauseMsString = "";
        public string PauseMsString { get => _pauseMsString; set => SetProperty(ref _pauseMsString, value); }

        public StepEditorViewModel(int nextStepNumber, Action<RecipeStep?> closeAction)
        {
            _closeAction = closeAction;
            CurrentStep = new RecipeStep { StepNumber = nextStepNumber };
            InitializeStringProperties();
            InitializeCommands();
        }

        public StepEditorViewModel(RecipeStep stepToEdit, Action<RecipeStep?> closeAction)
        {
            _closeAction = closeAction;
            CurrentStep = new RecipeStep
            {
                Id = stepToEdit.Id,
                RecipeId = stepToEdit.RecipeId,
                StepNumber = stepToEdit.StepNumber,
                Function = stepToEdit.Function,
                SpeedRPM = stepToEdit.SpeedRPM,
                Direction = stepToEdit.Direction,
                TargetXDeg = stepToEdit.TargetXDeg,
                Repeats = stepToEdit.Repeats,
                PauseMs = stepToEdit.PauseMs
            };
            InitializeStringProperties();
            InitializeCommands();
        }

        private void InitializeStringProperties()
        {
            StepNumberString = CurrentStep.StepNumber.ToString();
            SpeedRpmString = CurrentStep.SpeedRPM?.ToString() ?? "";
            TargetXDegString = CurrentStep.TargetXDeg?.ToString() ?? "";
            RepeatsString = CurrentStep.Repeats?.ToString() ?? "";
            PauseMsString = CurrentStep.PauseMs?.ToString() ?? "";
        }

        private void InitializeCommands()
        {
            SaveStepCommand = new RelayCommand(SaveStep);
            CancelCommand = new RelayCommand(Cancel);
        }

        public IRelayCommand SaveStepCommand { get; private set; } = null!;
        public IRelayCommand CancelCommand { get; private set; } = null!;

        private void SaveStep()
        {
            if (int.TryParse(StepNumberString, out var stepNum)) CurrentStep.StepNumber = stepNum;
            if (int.TryParse(SpeedRpmString, out var speed)) CurrentStep.SpeedRPM = speed; else CurrentStep.SpeedRPM = null;
            if (int.TryParse(TargetXDegString, out var target)) CurrentStep.TargetXDeg = target; else CurrentStep.TargetXDeg = null;
            if (int.TryParse(RepeatsString, out var repeats)) CurrentStep.Repeats = repeats; else CurrentStep.Repeats = null;
            if (int.TryParse(PauseMsString, out var pause)) CurrentStep.PauseMs = pause; else CurrentStep.PauseMs = null;
            _closeAction?.Invoke(CurrentStep);
        }

        private void Cancel()
        {
            _closeAction?.Invoke(null);
        }
    }
}