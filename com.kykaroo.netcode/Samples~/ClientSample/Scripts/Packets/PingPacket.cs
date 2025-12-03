using System.IO;
using NetcodePackage.Runtime;

namespace NetcodePackage.Samples.Example.Scripts.Packets
{
    public class PingPacket : INetworkPacket
    {
        public ushort Id => 5;

        public void Serialize(BinaryWriter w) { }

        public void Deserialize(BinaryReader r) { }
    }
}