using System.IO;
using Netcode;

namespace ClientSample
{
    public class PingPacket : INetworkPacket
    {
        public ushort Id => 5;

        public void Serialize(BinaryWriter w) { }

        public void Deserialize(BinaryReader r) { }
    }
}