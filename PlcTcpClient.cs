using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LM01_UI.Services; // Dodamo using za PlcService

// POPRAVEK: Vrnemo razred v glavni imenski prostor LM01_UI
namespace LM01_UI
{
    public class PlcTcpClient
    {
        private TcpClient _client;
        private NetworkStream? _stream;
        public bool IsConnected => _client?.Connected ?? false;

        public event Action<string>? LogMessageGenerated;
        public event Action<bool>? ConnectionStatusChanged;

        public PlcTcpClient()
        {
            _client = new TcpClient();
        }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            if (IsConnected) return;
            try
            {
                LogMessageGenerated?.Invoke($"Povezovanje na {ipAddress}:{port}...");
                // Uporabimo CancellationToken za timeout pri povezovanju
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _client.ConnectAsync(ipAddress, port, cts.Token);
                _stream = _client.GetStream();
                ConnectionStatusChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"Napaka pri povezovanju: {ex.Message}");
                Disconnect(); // Ob napaki kličemo Disconnect, da počistimo stanje
            }
        }

        public void Disconnect()
        {
            if (!IsConnected && _client.Client == null) return;
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            finally
            {
                _client = new TcpClient();
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        public async Task SendAsync(string data)
        {
            if (!IsConnected || _stream is null) throw new InvalidOperationException("Ni povezave s PLC.");

            byte[] bytes = Encoding.ASCII.GetBytes(data.PadRight(251, '0'));
            await _stream.WriteAsync(bytes, 0, bytes.Length);
            LogMessageGenerated?.Invoke($"Poslano: {data}");
        }

        public async Task<string> ReceiveAsync(CancellationToken token = default)
        {
            if (!IsConnected || _stream is null) return string.Empty;

            byte[] buffer = new byte[251];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).TrimEnd('\0', ' ');
        }

        public async Task<string> SendReceiveAsync(string data, TimeSpan timeout)
        {
            await SendAsync(data);
            using var cts = new CancellationTokenSource(timeout);
            try
            {
                return await ReceiveAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("PLC ni odgovoril v pričakovanem času.");
            }
        }
    }
}