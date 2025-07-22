using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LM01_UI.Services
{
    public class PlcTcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // Notranji pomnilnik za zadnje naložene parametre
        private string _lastParameters = new string('\0', 252);

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

        // Nova metoda za brisanje shranjenih parametrov
        public void ClearParameters()
        {
            _lastParameters = new string('\0', 252);
        }

        // Nova, posebna metoda za pošiljanje LOAD ukaza, ki sprejme samo parametre
        public async Task<string> SendLoadCommandAsync(string parameters, TimeSpan timeout)
        {
            // Preverimo, če je dolžina parametrov pravilna
            if (parameters.Length != 252)
                throw new ArgumentException("Parameter string must be 252 characters long.");

            // Posodobimo notranji pomnilnik in sestavimo celoten ukaz
            _lastParameters = parameters;
            string fullCommand = PlcService.LoadCommandPrefix + _lastParameters;

            // Uporabimo zasebno metodo za pošiljanje in prejemanje
            return await InternalSendReceiveAsync(fullCommand, timeout);
        }

        // Ta metoda sedaj sprejme samo 4-znakovno kodo in sama doda parametre
        public async Task<string> SendReceiveAsync(string commandCode, TimeSpan timeout)
        {
            if (commandCode.Length > 4) // Preprost način za ugotavljanje, ali je to samo koda
                throw new ArgumentException("This method should be called with a 4-char command code.");

            string fullCommand = commandCode + _lastParameters;
            return await InternalSendReceiveAsync(fullCommand, timeout);
        }

        // Zasebna metoda, ki dejansko pošlje celoten 256-znakovni niz in preprečuje podvajanje kode
        private async Task<string> InternalSendReceiveAsync(string fullCommand, TimeSpan timeout)
        {
            await _lock.WaitAsync();
            try
            {
                if (!IsConnected || _stream == null)
                    throw new InvalidOperationException("Not connected to PLC.");

                var messageBytes = Encoding.ASCII.GetBytes(fullCommand);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                return await ReadFixedLengthAsync(10); // Odgovor je vedno 10 znakov
            }
            finally
            {
                _lock.Release();
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