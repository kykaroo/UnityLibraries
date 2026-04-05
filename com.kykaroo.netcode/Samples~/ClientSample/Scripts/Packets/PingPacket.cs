using System.IO;
using com.kykaroo.netcode.Runtime;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets
{
    public class PingPacket : INetworkPacket
    {
        public ushort Id => 5;

        public void Serialize(BinaryWriter w) { }

        public void Deserialize(BinaryReader r) { }
    }
}