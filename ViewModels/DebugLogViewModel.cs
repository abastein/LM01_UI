using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace LM01_UI.ViewModels
{
    public class DebugLogViewModel : ViewModelBase
    {
        private readonly Logger _logger;

        public ObservableCollection<string> Messages => _logger.Messages;

        public bool IsFrozen => _logger.IsFrozen;

        public IRelayCommand ToggleFreezeCommand { get; }

        public IRelayCommand ClearLogCommand { get; }

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

