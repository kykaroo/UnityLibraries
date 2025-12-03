using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using NetcodePackage.Runtime;
using NetcodePackage.Samples.Example.Scripts.Packets;
using Debug = UnityEngine.Debug;

namespace NetcodePackage.Samples.Example.Scripts
{
    public class GameClient : IDisposable
    {
        private readonly PacketRegistry _registry;
        private NetworkManager? _network;
        private TcpClient? _tcpClient;
        private UdpClient? _udpClient;

        private double _timeStamp;
        private bool _pingReceived;

        public bool IsConnected { get; private set; }
        public event Action OnConnected = delegate { };
        public event Action OnDisconnected = delegate { };

        public GameClient(PacketRegistry registry)
        {
            _registry = registry;
        }

        public async UniTask<bool> ConnectAsync(string host, int tcpPort, int udpPort)
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

                PingLoop().Forget();

                var idPacket = new GetIdPacket();
                
                _network.SendReliable(idPacket);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");

                return false;
            }
        }

        private async UniTaskVoid PingLoop()
        {
            var packet = new PingPacket();

            while (IsConnected)
            {
                _pingReceived = false;
                _timeStamp = Stopwatch.GetTimestamp();

                SendReliable(packet);

                var waitTask = UniTask.WaitUntil(() => _pingReceived);
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(2));

                var completed = await UniTask.WhenAny(waitTask, timeoutTask);

                if (completed == 1)
                {
                    Debug.LogWarning("[Ping] Timeout! No response from сервер.");
                }

                await UniTask.Delay(2000);
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

        public void SendReliable(INetworkPacket packet)
        {
            if (!IsConnected) return;

            _network?.SendReliable(packet);
        }

        public void SendUnreliable(INetworkPacket packet)
        {
            if (!IsConnected) return;

            _network?.SendUnreliable(packet);
        }

        public double ProcessPing()
        {
            var now = Stopwatch.GetTimestamp();
            var elapsedMs = (now - _timeStamp) * 1000.0 / Stopwatch.Frequency;

            _pingReceived = true;

            return elapsedMs;
        }
    }
}