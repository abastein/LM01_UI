using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LM01_UI // <--- Ta namespace je pravilen in potreben
{
    public partial class PlcTcpClient : IDisposable
    {
        // Dogodka deklarirana kot nullable
        public event Action<string>? LogMessageGenerated;
        public event Action<bool>? ConnectionStatusChanged;

        // Polja deklarirana kot nullable
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lock = new object();

        public bool IsConnected
        {
            get
            {
                // Zanesljivejši način preverjanja povezave
                // Uporabljamo ?. (null-conditional operator) za varno dostopanje
                return _client is not null && _client.Connected && !(_client.Client.Poll(1, SelectMode.SelectRead) && _client.Client.Available == 0);
            }
        }

        public PlcTcpClient() // Dodan prazen konstruktor, če je potreben
        {
            // Včasih se PlcTcpClient inicializira brez parametrov,
            // zato so _client, _stream, _cancellationTokenSource zgolj inicializirani na null.
            // Opozorila CS8618 so sedaj odpravljena, ker so nullable.
        }

        //public async Task ConnectAsync(string ip, int port)
        //{
        //    if (IsConnected)
        //    {
        //        LogMessageGenerated?.Invoke("Že povezan.");
        //        return;
        //    }

        //    Cleanup(); // Počistimo morebitne ostanke prejšnje povezave

        //    _cancellationTokenSource = new CancellationTokenSource();
        //    _client = new TcpClient();

        //    try
        //    {
        //        LogMessageGenerated?.Invoke($"Povezovanje na {ip}:{port}...");
        //        // Uporabi WaitAsync, kot si že imel, za timeout
        //        await _client.ConnectAsync(ip, port).WaitAsync(TimeSpan.FromSeconds(3d), _cancellationTokenSource.Token);
        //        _stream = _client.GetStream();

        //        LogMessageGenerated?.Invoke("Povezava uspešna.");
        //        ConnectionStatusChanged?.Invoke(true);

        //        // Zaženemo poslušanje v ozadju - '_ =' za zanemarjanje opozorila CS4014
        //        _ = Task.Run(ListenLoop);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessageGenerated?.Invoke($"Napaka pri povezovanju: {ex.Message}");
        //        Cleanup();
        //        ConnectionStatusChanged?.Invoke(false);
        //    }
        //}
        public async Task ConnectAsync(string ip, int port)
        {
            if (IsConnected)
            {
                LogMessageGenerated?.Invoke("Že povezan.");
                return;
            }

            Cleanup(); // Počistimo morebitne ostanke prejšnje povezave

            _cancellationTokenSource = new CancellationTokenSource();
            // SPREMENJENO: Inicializiraj TcpClient z eksplicitno AddressFamily (IPv4)
            _client = new TcpClient(AddressFamily.InterNetwork); // <--- KLJUČNA SPREMEMBA!

            try
            {
                LogMessageGenerated?.Invoke($"Povezovanje na {ip}:{port}...");
                // SPREMENJENO: ODSTRANI WaitAsync za diagnostiko
                await _client.ConnectAsync(ip, port); // <--- KLJUČNA SPREMEMBA!

                _stream = _client.GetStream();

                LogMessageGenerated?.Invoke("Povezava uspešna.");
                ConnectionStatusChanged?.Invoke(true);

                _ = Task.Run(ListenLoop);
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"Napaka pri povezovanju: {ex.Message}");
                Cleanup();
                ConnectionStatusChanged?.Invoke(false);
            }
        }


        public void Disconnect()
        {
            LogMessageGenerated?.Invoke("Prekinjam povezavo...");
            Cleanup();
            ConnectionStatusChanged?.Invoke(false);
        }

        private async Task ListenLoop()
        {
            var buffer = new byte[1025];
            var cts = _cancellationTokenSource; // Lokalna referenca

            // Preverimo za null, saj je _cancellationTokenSource lahko null, če povezava ni bila vzpostavljena
            if (cts is null || _stream is null) return;

            try
            {
                while (!cts.IsCancellationRequested && _client?.Connected == true) // Dodana preverba povezanosti
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead == 0)
                    {
                        LogMessageGenerated?.Invoke("Strežnik je prekinil povezavo.");
                        break;
                    }

                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    LogMessageGenerated?.Invoke($"Prejeto: {receivedData}");
                }
            }
            catch (OperationCanceledException)
            {
                // Normalna prekinitev, tiho izstopimo (ko se CancellationTokenSource prekine)
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"Napaka pri branju s strežnika: {ex.Message}");
            }
            finally // Uporabi finally za čiščenje ne glede na catch
            {
                // Ko pridemo iz zanke (bodisi zaradi napake, prekinitve ali normalnega izstopa), poskrbimo za čiščenje.
                Cleanup();
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        // *** MANJKAJOČA METODA ReceiveAsync ***
        public async Task<string> ReceiveAsync(CancellationToken token = default) // Z dodanim neobveznim parametrom
        {
            if (_stream is null || _client is null || !_client.Connected)
            {
                return string.Empty;
            }

            try
            {
                byte[] buffer = new byte[1024]; // Uporabi 1024 za standardno velikost
                // Uporabi podan token za prekinitev, če je na voljo
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                if (bytesRead == 0)
                {
                    LogMessageGenerated?.Invoke("Strežnik je zaprl povezavo (ReceiveAsync).");
                    Disconnect(); // Poskusi prekiniti povezavo
                    return string.Empty;
                }

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                return receivedMessage;
            }
            catch (OperationCanceledException)
            {
                LogMessageGenerated?.Invoke("ReceiveAsync: Operacija preklicana.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"ReceiveAsync napaka: {ex.Message}");
                Disconnect(); // Poskusi prekiniti povezavo ob napaki
                return string.Empty;
            }
        }
        // **********************************

        //public async Task SendAsync(string data)
        //{
        //    if (!IsConnected || _stream is null) // Preverimo tudi _stream
        //    {
        //        LogMessageGenerated?.Invoke("Napaka: Ni povezave za pošiljanje.");
        //        return;
        //    }
        //    try
        //    {
        //        byte[] bytes = Encoding.ASCII.GetBytes(data);
        //        await _stream.WriteAsync(bytes, 0, bytes.Length);
        //        LogMessageGenerated?.Invoke($"Poslano: {data}");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessageGenerated?.Invoke($"Napaka pri pošiljanju: {ex.Message}");
        //        Disconnect(); // Poskusi prekiniti povezavo ob napaki
        //    }
        //}

        public async Task SendAsync(string data)
        {
            if (!IsConnected || _stream is null)
            {
                LogMessageGenerated?.Invoke("Napaka: Ni povezave za pošiljanje.");
                return;
            }

            try
            {
                // --- ZAČETEK SPREMEMBE ---

                const int TargetLength = 251; // Definiramo ciljno dolžino

                // Uporabimo PadRight, da nizu na desni strani dodamo ničle do dolžine 251.
                // Če je niz že daljši ali enak, ga metoda pusti nespremenjenega.
                string paddedData = data.PadRight(TargetLength, '0');

                // Za pošiljanje uporabimo nov, podložen niz.
                byte[] bytes = Encoding.ASCII.GetBytes(paddedData);

                // --- KONEC SPREMEMBE ---

                await _stream.WriteAsync(bytes, 0, bytes.Length);

                // V log zapišemo originalno sporočilo, da je bolj pregledno.
                LogMessageGenerated?.Invoke($"Poslano: {data} (podloženo na {TargetLength} znakov)");
            }
            catch (Exception ex)
            {
                LogMessageGenerated?.Invoke($"Napaka pri pošiljanju: {ex.Message}");
                Disconnect();
            }
        }

        // Enotna metoda za čiščenje vseh virov
        private void Cleanup()
        {
            lock (_lock)
            {
                // Pokliči Cancel() pred Dispose(), da se sproži OperationCanceledException v ListenLoop
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _stream?.Dispose();
                _client?.Close(); // Close() bo tudi sprostil vire
                _stream = null;
                _client = null;
            }
        }

        public void Dispose()
        {
            Cleanup();
            // Pomembno: ne pokličemo GC.SuppressFinalize(this), ker imamo samo managed vire
        }
    }
}