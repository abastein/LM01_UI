using LM01_UI.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LM01_UI
{
    public class PlcTcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly Logger _logger;

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
                await _client.ConnectAsync(ipAddress, port);
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

        // POPRAVEK: Dodana ključna beseda 'public'
        public async Task SendAsync(string message)
        {
            if (!IsConnected || _stream == null) throw new InvalidOperationException("Not connected to PLC.");

            var trimmedMessage = message.TrimEnd('\0');
            _logger.Inform(0, $"CLIENT > {trimmedMessage}");

            var messageBytes = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }

        // POPRAVEK: Dodana ključna beseda 'public'
        public async Task<string> SendReceiveAsync(string message, TimeSpan timeout)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to PLC.");

            await SendAsync(message);

            var readTask = ReadFixedLengthAsync(10);
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(readTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("PLC response timed out.");
            }

            return await readTask;
        }

        private async Task<string> ReadFixedLengthAsync(int length)
        {
            if (_stream == null) throw new InvalidOperationException("Stream is not available.");
            var buffer = new byte[length];
            int totalBytesRead = 0;
            while (totalBytesRead < length)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalBytesRead, length - totalBytesRead);
                if (bytesRead == 0) throw new IOException("Connection closed prematurely.");
                totalBytesRead += bytesRead;
            }
            string response = Encoding.ASCII.GetString(buffer, 0, totalBytesRead);

            _logger.Inform(0, $"PLC    < {response}");

            return response;
        }
    }
}