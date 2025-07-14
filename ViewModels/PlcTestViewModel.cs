using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using LM01_UI; // Za PlcTcpClient in Logger

namespace LM01_UI.ViewModels
{
    public class PlcTestViewModel : ViewModelBase // Prepričajte se, da deduje iz ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

        // Dodane lastnosti za IP in Port
        private string _plcIpAddress = "10.100.1.113"; // Privzeti IP
        private int _plcPort = 2000;                   // Privzeti Port
        private bool _isConnectedToPlc = false;        // Status povezave

        private string _receivedString = "(none)";
        private string _sendText = "";
        private ObservableCollection<string> _logMessages = new();

        // **KLJUČNA SPREMEMBA: PRAVILEN KONSTRUKTOR Z 2 ARGUMENTOMA**
        public PlcTestViewModel(PlcTcpClient plcClient, Logger logger)
        {
            _plcClient = plcClient;
            _logger = logger;

            // Naročite se na dogodke PlcTcpClient, da logirate sporočila in status
            _plcClient.LogMessageGenerated += msg => _logger.Inform(1, msg, AddLogMessageToCollection);
            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged; // Nov dogodek handler

            // Inicializiraj ukaze
            ConnectCommand = new AsyncRelayCommand(ConnectToPlcAsync); // Nov ukaz
            DisconnectCommand = new RelayCommand(DisconnectFromPlc);     // Nov ukaz
            SendCommand = new AsyncRelayCommand(SendAsync);
            ClearLogCommand = new RelayCommand(ClearLog);              // Nov ukaz
            // ExitCommand je v MainWindowViewModel, ne tukaj

            // Zaženi povezovanje in polling ob inicializaciji ViewModela
            // Namesto takojšnjega povezovanja, zdaj čakamo na klic ConnectCommand
            // _ = _plcClient.ConnectAsync(_plcIpAddress, _plcPort);
            // _ = StartPollingAsync(); // To se bo klicalo po uspešni povezavi

            _logger.Inform(1, "PlcTestViewModel initialised (via AdminPage).", AddLogMessageToCollection);
        }

        // --- Dodane Bindable Properties ---
        public string PlcIpAddress
        {
            get => _plcIpAddress;
            set => SetProperty(ref _plcIpAddress, value);
        }

        public int PlcPort
        {
            get => _plcPort;
            set => SetProperty(ref _plcPort, value);
        }

        public bool IsConnectedToPlc
        {
            get => _isConnectedToPlc;
            private set => SetProperty(ref _isConnectedToPlc, value);
        }
        // --- Konec dodanih lastnosti ---

        // Bindable Properties (že obstoječe)
        public string ReceivedString
        {
            get => _receivedString;
            private set => SetProperty(ref _receivedString, value);
        }

        public string SendText
        {
            get => _sendText;
            set => SetProperty(ref _sendText, value);
        }

        public ObservableCollection<string> LogMessages
        {
            get => _logMessages;
            // Ne potrebujemo setProperty, saj je ObservableCollection sama po sebi opazljiva
        }

        // --- Dodani Commands ---
        public IAsyncRelayCommand ConnectCommand { get; }
        public IRelayCommand DisconnectCommand { get; }
        public IRelayCommand ClearLogCommand { get; }
        // --- Konec dodanih Commands ---

        // Commands (že obstoječe)
        public IAsyncRelayCommand SendCommand { get; }

        // --- Dodane Async Methods (za Connect/Disconnect) ---
        private async Task ConnectToPlcAsync()
        {
            if (IsConnectedToPlc)
            {
                _logger.Inform(1, "Že povezan s PLC.", AddLogMessageToCollection);
                return;
            }
            await _plcClient.ConnectAsync(PlcIpAddress, PlcPort);
            // Po ConnectAsync, status povezave bo posodobljen preko dogodka ConnectionStatusChanged
        }

        private void DisconnectFromPlc()
        {
            _plcClient.Disconnect();
            // Po Disconnect, status povezave bo posodobljen preko dogodka ConnectionStatusChanged
        }

        private void ClearLog()
        {
            LogMessages.Clear();
            _logger.Inform(1, "Log očiščen.", AddLogMessageToCollection);
        }
        // --- Konec dodanih Async Methods ---

        // Async Methods (že obstoječe)
        private async Task SendAsync()
        {
            if (!IsConnectedToPlc) // Pošlji le, če je povezan
            {
                _logger.Inform(2, "Napaka: Ni povezave s PLC za pošiljanje.", AddLogMessageToCollection);
                return;
            }
            string txt = SendText.Trim();
            if (string.IsNullOrEmpty(txt)) return;

            await _sendSemaphore.WaitAsync();
            try
            {
                _logger.Inform(1, $"Sending: {txt}", AddLogMessageToCollection);
                await _plcClient.SendAsync(txt);
                _logger.Inform(1, $"Sent: {txt}", AddLogMessageToCollection);
                SendText = ""; // Clear box
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Send Error: {ex.Message}", AddLogMessageToCollection);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task StartPollingAsync()
        {
            int delayTime = 200;
            while (IsConnectedToPlc) // Polling samo, če je povezan
            {
                try
                {
                    string msg = await _plcClient.ReceiveAsync();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        ReceivedString = msg;
                        _logger.Inform(1, $"Received: {msg}", AddLogMessageToCollection);
                        delayTime = 200;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Inform(2, $"Polling Error: {ex.Message}", AddLogMessageToCollection);
                    delayTime = 1000;
                }
                await Task.Delay(delayTime);
            }
        }

        // Handler za spremembo statusa povezave
        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            // Poskrbite, da se to izvede na UI niti
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsConnectedToPlc = isConnected;
                _logger.Inform(1, $"PLC povezava: {(isConnected ? "VZPOSTAVLJENA" : "PREKINJENA")}", AddLogMessageToCollection);

                // Zaženi polling, če je povezava vzpostavljena, ustavi, če je prekinjena
                if (isConnected)
                {
                    _ = StartPollingAsync(); // Zaženi polling v ozadju
                }
                // Ni potrebe po eksplicitnem ustavljanju, saj se zanka v StartPollingAsync konča, ko IsConnectedToPlc postane false
            });
        }

        // Metoda, ki jo bo klical Logger, da doda sporočila v kolekcijo LogMessages
        public void AddLogMessageToCollection(string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                LogMessages.Add(message);
            });
        }
    }
}