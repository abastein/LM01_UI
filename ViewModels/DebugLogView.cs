using System.Collections.ObjectModel;

namespace LM01_UI.ViewModels
{
    public class DebugLogViewModel : ViewModelBase
    {
        private readonly Logger _logger;

        public ObservableCollection<string> Messages => _logger.Messages;

        public DebugLogViewModel(Logger logger)
        {
            _logger = logger;
        }
    }
}
