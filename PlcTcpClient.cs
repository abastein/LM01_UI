using LM01_UI.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LM01_UI
{
    public class PlcTcpClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly Logger _logger;
        private readonly SemaphoreSlim _ioLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Expected length of responses from the PLC.
        /// </summary>
        private const int PlcResponseLength = 12;

        /// <summary>
        /// Terminator, ki ga PLC pričakuje; npr. "\r", "\n" ali String.Empty.
        /// Če PLC ne želi nobenih dodatnih znakov, nastavimo prazni niz.
        /// </summary>
        public string Terminator { get; set; } = string.Empty;

        public bool IsConnected { get; private set; }
        public event Action<bool>? ConnectionStatusChanged;

        public PlcTcpClient(Logger logger)
        {
            _logger = logger;
        }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            if (IsConnected) return;
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port).ConfigureAwait(false);
                _stream = _client.GetStream();
                IsConnected = true;
                _logger.Inform(1, $"Povezava s PLC ({ipAddress}:{port}) uspešno vzpostavljena.");
                ConnectionStatusChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.Inform(2, $"Povezava s PLC ni uspela: {ex.Message}");
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;
            IsConnected = false;
            _stream?.Dispose();
            _client?.Dispose();
            _logger.Inform(1, "Povezava s PLC prekinjena.");
            ConnectionStatusChanged?.Invoke(false);
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to PLC.");

            await _ioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SendInternalAsync(message).ConfigureAwait(false);
            }
            finally
            {
                _ioLock.Release();
            }
        }

        private async Task SendInternalAsync(string message)
        {
            // Odstrani morebitne končnice za pravilen log
            var clean = message.TrimEnd('\0', '\r', '\n');
            _logger.Inform(0, $"CLIENT > {clean}");

            // Sestavi natančen paket brez dodatnih terminatorjev, če PLC ne želi nobenih
            var packet = clean + Terminator;
            var bytes = Encoding.ASCII.GetBytes(packet);

            await _stream!.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        public async Task<string> SendReceiveAsync(string message, TimeSpan timeout)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to PLC.");

            await _ioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SendInternalAsync(message).ConfigureAwait(false);

                var readTask = ReadFixedLengthAsync();
                var timeoutTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    //throw new TimeoutException("PLC response timed out.");
                    return null;
                }

                return await readTask.ConfigureAwait(false);
            }
            finally
            {
                _ioLock.Release();
            }
        }

        private async Task<string> ReadFixedLengthAsync()
        {
            if (_stream == null) throw new InvalidOperationException("Stream is not available.");
            var buffer = new byte[PlcResponseLength];
            int totalBytesRead = 0;
            while (totalBytesRead < PlcResponseLength)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalBytesRead, PlcResponseLength - totalBytesRead).ConfigureAwait(false);
                if (bytesRead == 0) throw new IOException("Connection closed prematurely.");
                totalBytesRead += bytesRead;
            }

            // Odstrani končnice iz odgovora
            string response = Encoding.ASCII.GetString(buffer, 0, totalBytesRead)
                .TrimEnd('\r', '\n');

            _logger.Inform(0, $"PLC    < {response}");

            return response;
        }

        public void Dispose()
        {
            _ioLock.Dispose();
            Disconnect();
        }
    }
}
