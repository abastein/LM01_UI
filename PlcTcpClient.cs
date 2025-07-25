using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LM01_UI.Services
{
    public class PlcTcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;

        public bool IsConnected { get; private set; }
        public event Action<bool>? ConnectionStatusChanged;

        public async Task ConnectAsync(string ipAddress, int port)
        {
            if (IsConnected) return;
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);
                _stream = _client.GetStream();
                IsConnected = true;
                ConnectionStatusChanged?.Invoke(true);
            }
            catch (Exception)
            {
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
            ConnectionStatusChanged?.Invoke(false);
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to PLC.");

            var messageBytes = Encoding.ASCII.GetBytes(message);
            await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }

        public async Task<string> SendReceiveAsync(string message, TimeSpan timeout)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to PLC.");

            await SendAsync(message);

            var readTask = ReadFixedLengthAsync(12);
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(readTask, timeoutTask);

            if (completedTask == timeoutTask)
                throw new TimeoutException("PLC response timed out.");

            // Retrieve full 12‑char response
            string fullResponse = await readTask;   // e.g. "XX1234567890"

            // Drop the first two characters and return the last 10 chars
            return fullResponse[2..];               // returns "1234567890"
        }

        private async Task<string> ReadFixedLengthAsync(int length)
        {
            if (_stream == null)
                throw new InvalidOperationException("Stream is not available.");

            var buffer = new byte[length];
            int totalBytesRead = 0;

            while (totalBytesRead < length)
            {
                int bytesRead = await _stream.ReadAsync(buffer, totalBytesRead, length - totalBytesRead);
                if (bytesRead == 0)
                    throw new IOException("Connection closed prematurely.");

                totalBytesRead += bytesRead;
            }

            return Encoding.ASCII.GetString(buffer, 0, totalBytesRead);
        }
    }
}
