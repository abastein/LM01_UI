using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LM01_UI.Services;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace LM01_UI.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase, IDisposable
    {
        private readonly PlcTcpClient _plcClient;
        private readonly Logger _logger;
        private readonly Action<string> _navigate;

        [ObservableProperty]
        private string _plcStatusText = "PLC ni povezan";

        [ObservableProperty]
        private IBrush _plcStatusBrush = Brushes.IndianRed;

        [ObservableProperty]
        private bool _isPlcConnected;

        public WelcomeViewModel(PlcTcpClient plcClient, Logger logger, Action<string> navigate)
        {
            _plcClient = plcClient;
            _logger = logger;
            _navigate = navigate;

            ConnectToPlcCommand = new AsyncRelayCommand(ConnectToPlcAsync);

            _plcClient.ConnectionStatusChanged += OnPlcConnectionStatusChanged;
        }

        public IAsyncRelayCommand ConnectToPlcCommand { get; } 

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsPlcConnected = isConnected;

                if (isConnected)
                {
                    PlcStatusText = "PLC Povezan";
                    PlcStatusBrush = Brushes.MediumSeaGreen;
                    _logger.Inform(1, "Povezava s PLC uspešno vzpostavljena.");
                }
                else
                {
                    PlcStatusText = "PLC ni povezan";
                    PlcStatusBrush = Brushes.IndianRed;
                    _logger.Inform(2, "Povezava s PLC prekinjena.");
                }
            });
        }

        [RelayCommand]
        private void NavigateToRun() => _navigate("Run");

        [RelayCommand]
        private void NavigateToAdmin() => _navigate("Admin");

        private async Task ConnectToPlcAsync()
        {
            try
            {
                if (!_plcClient.IsConnected)
                {
                    var ipAddress = GetPlcIpAddress();
                    await _plcClient.ConnectAsync(ipAddress, 2000);
                }
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri povezovanju s PLC: {ex.Message}");
            }
        }



        private string GetPlcIpAddress()
        {
            try
            {
                var searchedLocations = new List<string>();
                var candidateDirectories = GetCandidateConfigDirectories();

                foreach (var directory in candidateDirectories)
                {
                    var configPath = Path.Combine(directory, "PLCIP.txt");
                    searchedLocations.Add(configPath);

                    if (!File.Exists(configPath))
                    {
                        continue;
                    }

                    var ipAddress = File.ReadAllText(configPath).Trim();

                    if (string.IsNullOrWhiteSpace(ipAddress))
                    {
                        throw new InvalidDataException("Datoteka s PLC IP naslovom je prazna.");
                    }

                    if (!IPAddress.TryParse(ipAddress, out _))
                    {
                        throw new InvalidDataException("Vrednost IP naslova PLC ni veljavna IPv4 ali IPv6 vrednost.");
                    }

                    _logger.Inform(1, $"Prebran PLC IP naslov iz: {configPath}");
                    return ipAddress;
                }

                var errorMessage = "Datoteka s PLC IP naslovom ni bila najdena v naslednjih lokacijah:" + Environment.NewLine + string.Join(Environment.NewLine, searchedLocations);
                throw new FileNotFoundException(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Napaka pri branju IP naslova PLC: {ex.Message}");
                throw;
            }
        }

        private static IEnumerable<string> GetCandidateConfigDirectories()
        {
            static IEnumerable<string> EnumerateSelfAndParents(string? path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    yield break;
                }

                var current = Path.GetFullPath(path);

                while (!string.IsNullOrWhiteSpace(current))
                {
                    yield return current;

                    var parent = Path.GetDirectoryName(current);

                    if (string.IsNullOrWhiteSpace(parent) || parent == current)
                    {
                        break;
                    }

                    current = parent;
                }
            }

            var candidateRoots = new[]
            {
                AppContext.BaseDirectory,
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty)
            };

            return candidateRoots
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .SelectMany(EnumerateSelfAndParents)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }


        public void ResetToStartupState()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_plcClient.IsConnected)
                {
                    IsPlcConnected = true;
                    PlcStatusText = "PLC Povezan";
                    PlcStatusBrush = Brushes.MediumSeaGreen;
                }
                else
                {
                    IsPlcConnected = false;
                    PlcStatusText = "PLC ni povezan";
                    PlcStatusBrush = Brushes.IndianRed;
                }
            });
        }


        public void Dispose()
        {
            _plcClient.ConnectionStatusChanged -= OnPlcConnectionStatusChanged;
        }
    }
}