using System.Net;

namespace com.kykaroo.netcode.Runtime
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

        public void EnqueueReliable(INetworkPacket packet) => _ = _tcp.EnqueueSendAsync(packet);
        public void SendImmediate(INetworkPacket packet) => _ = _tcp.SendImmediateAsync(packet);
        public void FlushReliable() => _tcp.Flush();
        public void SendUnreliable(INetworkPacket packet) => _ = _udp.SendAsync(packet);

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