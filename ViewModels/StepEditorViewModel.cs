using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace LM01_UI.ViewModels
{
    public class FunctionViewModel
    {
        public EStepFunction Function { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public partial class StepEditorViewModel : ViewModelBase
    {
        private readonly RecipeStep _step;

        public ObservableCollection<FunctionViewModel> AvailableFunctions { get; }

        [ObservableProperty]
        private FunctionViewModel? _selectedFunction;

        [ObservableProperty]
        private int? _speedRPM;

        [ObservableProperty]
        private int? _targetXDeg;

        [ObservableProperty]
        private int? _repeats;

        [ObservableProperty]
        private int? _pauseMs;

        [ObservableProperty]
        private EDirection _direction;

        public StepEditorViewModel(RecipeStep step)
        {
            _step = step;
            AvailableFunctions = new ObservableCollection<FunctionViewModel>
            {
                new FunctionViewModel { Function = EStepFunction.Rotate, Name = "Vrtenje" },
                new FunctionViewModel { Function = EStepFunction.Wait, Name = "Pavza" },
                new FunctionViewModel { Function = EStepFunction.Repeat, Name = "Ponovi" }
            };

            LoadStepProperties();
            SaveChangesCommand = new RelayCommand(SaveChanges);
        }

        private void LoadStepProperties()
        {
            if (_step != null)
            {
                SelectedFunction = AvailableFunctions.FirstOrDefault(f => f.Function == _step.Function);
                Direction = _step.Direction;
                SpeedRPM = _step.SpeedRPM;
                TargetXDeg = _step.TargetXDeg;
                Repeats = _step.Repeats;
                PauseMs = _step.PauseMs;
            }
        }

        public IRelayCommand SaveChangesCommand { get; }
        private void SaveChanges()
        {
            if (_step != null && SelectedFunction != null)
            {
                _step.Function = SelectedFunction.Function;
                _step.Direction = Direction;
                _step.SpeedRPM = SpeedRPM ?? 0;
                _step.TargetXDeg = TargetXDeg ?? 0;
                _step.Repeats = Repeats ?? 0;
                _step.PauseMs = PauseMs ?? 0;
            }
        }
    }
}