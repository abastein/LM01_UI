using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Enums;
using LM01_UI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public partial class StepEditorViewModel : ViewModelBase
    {
        // POPRAVEK: Pravilen tip za closeAction
        private readonly Action<RecipeStep?> _closeAction;
        public RecipeStep CurrentStep { get; private set; }

        public bool IsSpeedRpmEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsDirectionEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsTargetXDegEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsRepeatsEnabled => SelectedFunction?.Function == FunctionType.Repeat;
        public bool IsPauseMsEnabled => SelectedFunction?.Function == FunctionType.Wait;

        [ObservableProperty]
        private string _stepNumberString = "";
        [ObservableProperty]
        private string _speedRpmString = "";
        [ObservableProperty]
        private string _targetXDegString = "";
        [ObservableProperty]
        private string _repeatsString = "";
        [ObservableProperty]
        private string _pauseMsString = "";

        public List<FunctionViewModel> FunctionTypes { get; }
        public Array DirectionTypes { get; } = Enum.GetValues(typeof(DirectionType));

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSpeedRpmEnabled))]
        [NotifyPropertyChangedFor(nameof(IsDirectionEnabled))]
        [NotifyPropertyChangedFor(nameof(IsTargetXDegEnabled))]
        [NotifyPropertyChangedFor(nameof(IsRepeatsEnabled))]
        [NotifyPropertyChangedFor(nameof(IsPauseMsEnabled))]
        private FunctionViewModel? _selectedFunction;

        public StepEditorViewModel(RecipeStep step, Action<RecipeStep?> closeAction)
        {
            _closeAction = closeAction;
            CurrentStep = step;

            FunctionTypes = new List<FunctionViewModel>
            {
                new FunctionViewModel { Function = FunctionType.Rotate, Name = "Vrtenje" },
                new FunctionViewModel { Function = FunctionType.Wait, Name = "Pavza" },
                new FunctionViewModel { Function = FunctionType.Repeat, Name = "Ponovi" }
            };

            InitializeStringProperties();
            InitializeCommands();
        }

        private void InitializeStringProperties()
        {
            StepNumberString = CurrentStep.StepNumber.ToString();
            SpeedRpmString = CurrentStep.SpeedRPM.ToString();
            TargetXDegString = CurrentStep.TargetXDeg.ToString();
            RepeatsString = CurrentStep.Repeats.ToString();
            PauseMsString = CurrentStep.PauseMs.ToString();
            SelectedFunction = FunctionTypes.FirstOrDefault(f => f.Function == CurrentStep.Function);
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
            if (SelectedFunction != null) CurrentStep.Function = SelectedFunction.Function;
            if (int.TryParse(StepNumberString, out var stepNum)) CurrentStep.StepNumber = stepNum;
            if (int.TryParse(SpeedRpmString, out var speed)) CurrentStep.SpeedRPM = speed;
            if (int.TryParse(TargetXDegString, out var target)) CurrentStep.TargetXDeg = target;
            if (int.TryParse(RepeatsString, out var repeats)) CurrentStep.Repeats = repeats;
            if (int.TryParse(PauseMsString, out var pause)) CurrentStep.PauseMs = pause;

            _closeAction?.Invoke(CurrentStep);
        }

        private void Cancel()
        {
            _closeAction?.Invoke(null);
        }
    }
}