using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Enums;
using LM01_UI.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public partial class StepEditorViewModel : ViewModelBase
    {
        private readonly RecipeStep _step;
        private readonly Action<RecipeStep?> _closeAction;

        public ObservableCollection<FunctionViewModel> FunctionTypes { get; }
        public Array DirectionTypes { get; } = Enum.GetValues(typeof(DirectionType));

        // --- POPRAVEK: Vse lastnosti so napisane ročno, brez [ObservableProperty] ---

        private FunctionViewModel? _selectedFunction;
        public FunctionViewModel? SelectedFunction
        {
            get => _selectedFunction;
            set
            {
                if (SetProperty(ref _selectedFunction, value))
                {
                    OnPropertyChanged(nameof(IsSpeedRpmEnabled));
                    OnPropertyChanged(nameof(IsDirectionEnabled));
                    OnPropertyChanged(nameof(IsTargetXDegEnabled));
                    OnPropertyChanged(nameof(IsRepeatsEnabled));
                    OnPropertyChanged(nameof(IsPauseMsEnabled));
                }
            }
        }

        private string _speedRpmString = string.Empty;
        public string SpeedRpmString
        {
            get => _speedRpmString;
            set => SetProperty(ref _speedRpmString, value);
        }

        private string _targetXDegString = string.Empty;
        public string TargetXDegString
        {
            get => _targetXDegString;
            set => SetProperty(ref _targetXDegString, value);
        }

        private string _repeatsString = string.Empty;
        public string RepeatsString
        {
            get => _repeatsString;
            set => SetProperty(ref _repeatsString, value);
        }

        private string _pauseMsString = string.Empty;
        public string PauseMsString
        {
            get => _pauseMsString;
            set => SetProperty(ref _pauseMsString, value);
        }

        private DirectionType _direction;
        public DirectionType Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        public string StepNumberString { get; }

        public bool IsSpeedRpmEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsDirectionEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsTargetXDegEnabled => SelectedFunction?.Function == FunctionType.Rotate;
        public bool IsRepeatsEnabled => SelectedFunction?.Function == FunctionType.Repeat;
        public bool IsPauseMsEnabled => SelectedFunction?.Function == FunctionType.Wait;

        public IRelayCommand SaveStepCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public StepEditorViewModel(RecipeStep step, Action<RecipeStep?> closeAction)
        {
            _step = step;
            _closeAction = closeAction;

            StepNumberString = _step.StepNumber.ToString();

            FunctionTypes = new ObservableCollection<FunctionViewModel>
            {
                new FunctionViewModel { Function = FunctionType.Rotate, Name = "Vrtenje" },
                new FunctionViewModel { Function = FunctionType.Wait, Name = "Pavza" },
                new FunctionViewModel { Function = FunctionType.Repeat, Name = "Ponovi" }
            };

            LoadStepProperties();
            SaveStepCommand = new RelayCommand(SaveStep);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void LoadStepProperties()
        {
            SelectedFunction = FunctionTypes.FirstOrDefault(f => f.Function == _step.Function);
            Direction = _step.Direction;
            SpeedRpmString = _step.SpeedRPM.ToString();
            TargetXDegString = _step.TargetXDeg.ToString();
            RepeatsString = _step.Repeats.ToString();
            PauseMsString = _step.PauseMs.ToString();
        }

        private void SaveStep()
        {
            if (SelectedFunction != null) _step.Function = SelectedFunction.Function;
            _step.Direction = Direction;

            if (int.TryParse(SpeedRpmString, out var speed)) _step.SpeedRPM = speed;
            if (int.TryParse(TargetXDegString, out var target)) _step.TargetXDeg = target;
            if (int.TryParse(RepeatsString, out var repeats)) _step.Repeats = repeats;
            if (int.TryParse(PauseMsString, out var pause)) _step.PauseMs = pause;

            _closeAction(_step);
        }

        private void Cancel()
        {
            _closeAction(null);
        }
    }
}