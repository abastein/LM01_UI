using CommunityToolkit.Mvvm.Input;

namespace LM01_UI.ViewModels
{
    public class PranjeViewModel : ViewModelBase
    {
        public PranjeViewModel()
        {
            NormalnoPranjeCommand = new RelayCommand(() => { });
            IntenzivnoPranjeCommand = new RelayCommand(() => { });
        }

        public IRelayCommand NormalnoPranjeCommand { get; }
        public IRelayCommand IntenzivnoPranjeCommand { get; }
    }
}
