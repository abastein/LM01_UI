using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LM01_UI.ViewModels
{
    public partial class PlcTestViewModel : ViewModelBase
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;

        [ObservableProperty]
        private string _commandToSend = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _logMessages;

        public PlcTestViewModel(PlcTcpClient plcClient, Logger logger)
        {
            _plcClient = plcClient;
            _logger = logger;

            // This now works because the Logger class has a public 'Messages' property.
            LogMessages = _logger.Messages;
        }

        [RelayCommand]
        private async Task SendCommand()
        {
            if (!_plcClient.IsConnected || string.IsNullOrEmpty(CommandToSend))
            {
                return;
            }

            try
            {
                await _plcClient.SendAsync(CommandToSend);
                _logger.Inform(1, "Command sent; no immediate response expected.");
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"ERROR: {ex.Message}");
            }
        }
    }
}