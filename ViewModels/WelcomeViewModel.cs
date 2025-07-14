using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.ViewModels; // Prepričaj se, da je to prisotno, če je ViewModelBase v istem namespace-u ali ga ustrezno prilagodi.
using System;
using System.ComponentModel; // Ni več nujno, če podeduješ iz ViewModelBase
using System.Runtime.CompilerServices; // Ni več nujno, če podeduješ iz ViewModelBase
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    // Podeduj iz ViewModelBase
    public class WelcomeViewModel : ViewModelBase // Prej je bil : INotifyPropertyChanged, sedaj : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private Action<object>? _navigateAction;

        private bool _isPlcConnected;
        private string _plcStatusText = "Povezava s PLC: Prekinjena";

        public WelcomeViewModel(PlcTcpClient plcClient, Logger logger, Action<object> navigateAction)
        {
            _plcClient = plcClient;
            _logger = logger;
            _navigateAction = navigateAction;

            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged;

            NavigateToRunCommand = new RelayCommand(() => _navigateAction?.Invoke("Run"));
            NavigateToAdminCommand = new RelayCommand(() => _navigateAction?.Invoke("Admin"));

            _logger.Inform(1, "WelcomeViewModel initialised.");

            _ = Task.Run(async () => await _plcClient.ConnectAsync("10.100.1.113", 2000));
        }

        public bool IsPlcConnected
        {
            get => _isPlcConnected;
            private set
            {
                if (SetProperty(ref _isPlcConnected, value)) // SetProperty je zdaj iz ViewModelBase
                {
                    PlcStatusText = value ? "Povezava s PLC: VZPOSTAVLJENA" : "Povezava s PLC: Prekinjena";
                }
            }
        }

        public string PlcStatusText
        {
            get => _plcStatusText;
            private set => SetProperty(ref _plcStatusText, value); // SetProperty je zdaj iz ViewModelBase
        }

        public IRelayCommand NavigateToRunCommand { get; }
        public IRelayCommand NavigateToAdminCommand { get; }

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsPlcConnected = isConnected;
                _logger.Inform(1, $"PLC povezava status: {(isConnected ? "VZPOSTAVLJENA" : "PREKINJENA")}");
            });
        }

        // ODSTRANITE TA DVA BLOKA, KI STA SEDAJ V VIEWMODELBASE.CS:
        // public event PropertyChangedEventHandler? PropertyChanged;
        // protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null) { ... }
    }
}