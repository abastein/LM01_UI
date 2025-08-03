using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LM01_UI;

namespace LM01_UI.Services
{
    /// <summary>
    /// Continuously polls the PLC for status updates and raises an event
    /// whenever fresh data is received.
    /// </summary>
    public class PlcStatusService : IDisposable
    {
        private readonly PlcTcpClient _tcpClient;
        private readonly PlcService _plcService;
        private readonly Logger _logger;
        private CancellationTokenSource? _cts;

        public event EventHandler<PlcStatusEventArgs>? StatusUpdated;

        public PlcStatusService(PlcTcpClient tcpClient, PlcService plcService, Logger logger)
        {
            _tcpClient = tcpClient;
            _plcService = plcService;
            _logger = logger;
        }

        /// <summary>
        /// Starts the background polling loop.
        /// </summary>
        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            _ = PollLoop(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task PollLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _tcpClient.IsConnected)
                {
                    try
                    {
                        var command = _plcService.GetStatusCommand();
                        string response = await _tcpClient.SendReceiveAsync(
                            command,
                            TimeSpan.FromSeconds(0.5));
                        _logger.Inform(0, $"STATUS → sent: {command}");
                        _logger.Inform(0, $"STATUS ← response: {response}");

                        var status = ParseStatus(response);
                        if (status != null)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                _logger.Inform(0, $"STATUS event: state={status.State}, recipe={status.LoadedRecipeId}, step={status.Step}, error={status.ErrorCode}");
                                StatusUpdated?.Invoke(this, new PlcStatusEventArgs(status));
                            });
                        }
                    }
                    catch (TimeoutException)
                    {
                        _logger.Inform(0, "STATUS timeout");
                        // ignore timeouts, they will be retried on next loop
                    }
                    catch (Exception ex)
                    {
                        _logger.Inform(0, $"STATUS exception: {ex.Message}");
                        // any other exception terminates polling
                        _tcpClient.Disconnect();
                        break;
                    }

                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException)
            {
                // polling cancelled
            }
            finally
            {
                Stop();
            }
        }

        private PlcStatus? ParseStatus(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                _logger.Inform(0, "STATUS parsed FAIL");
                return null;
            }

            var digits = new string(response.Where(char.IsDigit).ToArray());
            if (digits.Length >= 10)
                digits = digits[^10..];
            if (digits.Length < 10)
            {
                _logger.Inform(0, "STATUS parsed FAIL");
                return null;
            }

            _logger.Inform(0, "STATUS parsed OK");
            return new PlcStatus
            {
                Raw = response,
                State = digits.Substring(0, 1),
                LoadedRecipeId = int.Parse(digits.Substring(1, 3)),
                Step = int.Parse(digits.Substring(4, 2)),
                ErrorCode = int.Parse(digits.Substring(6, 4))
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class PlcStatus
    {
        public string State { get; init; } = string.Empty;
        public int LoadedRecipeId { get; init; }
        public int Step { get; init; }
        public int ErrorCode { get; init; }
        public string Raw { get; init; } = string.Empty;
    }

    public class PlcStatusEventArgs : EventArgs
    {
        public PlcStatusEventArgs(PlcStatus status)
        {
            Status = status;
        }

        public PlcStatus Status { get; }
    }
}
