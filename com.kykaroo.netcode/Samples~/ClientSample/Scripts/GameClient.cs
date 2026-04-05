using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ClientSample
{
    public class GameClient : IDisposable
    {
        private readonly PacketRegistry _registry;
        private NetworkManager? _network;
        private TcpClient? _tcpClient;
        private UdpClient? _udpClient;

        private double _timeStamp;

        private TaskCompletionSource<double> _pingTcs;

        public bool IsConnected { get; private set; }
        public event Action OnConnected = delegate { };
        public event Action OnDisconnected = delegate { };
        public event Action<double> OnPing = delegate { };

        public GameClient(PacketRegistry registry)
        {
            _registry = registry;

            Application.quitting += () =>
            {
                if (IsConnected)
                {
                    Disconnect();
                }
            };
        }

        public async Task<bool> ConnectAsync(string host, int tcpPort, int udpPort)
        {
            if (IsConnected)
            {
                Debug.LogError("Already connected!");

                return false;
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var ip = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);

                _tcpClient = new TcpClient(AddressFamily.InterNetwork);
                _tcpClient.NoDelay = true;
                _tcpClient.Client.NoDelay = true;
                _tcpClient.Client.SendBufferSize = 8192;
                _tcpClient.Client.ReceiveBufferSize = 8192;

                await _tcpClient.ConnectAsync(ip, tcpPort);

                _udpClient = new UdpClient();
                var udpEndPoint = new IPEndPoint(ip, udpPort);

                var tcp = new TcpChannel(_tcpClient, _registry);
                var udp = new UdpChannel(_udpClient, udpEndPoint, _registry);

                _network = new NetworkManager(tcp, udp, _registry);

                _network.StartListening();

                IsConnected = true;

                OnConnected();

                Debug.Log("Connected to server.");

                _ = PingLoop();

                var idPacket = new GetIdPacket();

                _network.EnqueueReliable(idPacket);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");

                return false;
            }
        }

        private async Task PingLoop()
        {
            var packet = new PingPacket();

            while (IsConnected)
            {
                _pingTcs = new TaskCompletionSource<double>();
                _timeStamp = Stopwatch.GetTimestamp();

                SendImmediate(packet);

                var timeoutTask = Task.Delay(10000);

                var completed = await Task.WhenAny(_pingTcs.Task, timeoutTask);

                if (completed == timeoutTask)
                {
                    Debug.LogWarning("[Ping] Timeout! No response from server.");

                    continue;
                }

                var ping = await _pingTcs.Task;
                OnPing(ping);

                await Task.Delay(300);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            Debug.Log("Disconnecting from server...");

            try
            {
                _network?.StopListening();

                _tcpClient?.Close();
                _udpClient?.Close();

                _tcpClient?.Dispose();
                _udpClient?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during disconnect: {e.Message}");
            }
            finally
            {
                IsConnected = false;
                _network = null;
                _tcpClient = null;
                _udpClient = null;

                OnDisconnected?.Invoke();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void EnqueueReliable(INetworkPacket packet)
        {
            if (!IsConnected) return;

            _network?.EnqueueReliable(packet);
        }

        public void SendImmediate(INetworkPacket packet)
        {
            if (!IsConnected) return;

            _network?.SendImmediate(packet);
        }

        public void FlushReliable()
        {
            _network?.FlushReliable();
        }

        public void SendUnreliable(INetworkPacket packet)
        {
            if (!IsConnected) return;

            _network?.SendUnreliable(packet);
        }

        public void ProcessPing()
        {
            var now = Stopwatch.GetTimestamp();

            var elapsedMs = (now - _timeStamp) * 1000.0 / Stopwatch.Frequency;

            _pingTcs?.TrySetResult(elapsedMs);
        }
    }
}