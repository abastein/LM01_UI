using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace LM01_UI.ViewModels
{
    public class DebugLogViewModel : ViewModelBase
    {
        private readonly Logger _logger;

        public ObservableCollection<string> Messages => _logger.Messages;

        public bool IsFrozen => _logger.IsFrozen;

        public ICommand ToggleFreezeCommand { get; }

        public ICommand ClearLogCommand { get; }

        public DebugLogViewModel(Logger logger)
        {
            _logger = logger;
            ToggleFreezeCommand = new RelayCommand(() =>
            {
                _logger.ToggleFreeze();
                OnPropertyChanged(nameof(IsFrozen));
            });

            ClearLogCommand = new RelayCommand(() =>
            {
                _logger.Clear();
            });
        }
    }
}

