using System.Net;

namespace NetcodePackage.Runtime
{
    public class NetworkManager
    {
        private readonly TcpChannel _tcp;
        private readonly UdpChannel _udp;
        private readonly PacketRegistry _registry;

        public NetworkManager(TcpChannel tcp, UdpChannel udp, PacketRegistry registry)
        {
            _tcp = tcp;
            _udp = udp;
            _registry = registry;
        }

        public void SendReliable(INetworkPacket packet) => _tcp.SendAsync(packet).Forget();
        public void SendUnreliable(INetworkPacket packet) => _udp.SendAsync(packet).Forget();

        public void StartListening()
        {
            _tcp.StartListening(OnTcpPacket);
            _udp.StartListening(OnUdpPacket);
        }

        public void StopListening()
        {
            _tcp.Stop();
            _udp.Stop();
        }

        private void OnTcpPacket(INetworkPacket packet)
        {
            _registry.Handle(packet);
        }

        private void OnUdpPacket(IPEndPoint sender, INetworkPacket packet)
        {
            _registry.Handle(packet);
        }
    }
}