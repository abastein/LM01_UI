using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading; // Potrebno za SemaphoreSlim
using System.Threading.Tasks;

namespace LM01_UI.Services
{
    public class PlcTcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

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

        public async Task<string> SendReceiveAsync(string message, TimeSpan timeout)
        {
            await _lock.WaitAsync(); // Počakamo na zeleno luč
            try
            {
                if (!IsConnected || _stream == null)
                    throw new InvalidOperationException("Not connected to PLC.");

                // 1. Pošljemo ukaz
                var messageBytes = Encoding.ASCII.GetBytes(message);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                // 2. Preberemo odgovor fiksne dolžine (10 znakov)
                var readTask = ReadFixedLengthAsync(10);
                var timeoutTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(readTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("PLC response timed out.");
                }

                return await readTask;
            }
            finally
            {
                _lock.Release(); // Sprostimo semafor za naslednji ukaz
            }
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
            return Encoding.ASCII.GetString(buffer, 0, totalBytesRead);
        }
    }
}