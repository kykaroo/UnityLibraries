using System.IO;
using com.kykaroo.netcode.Runtime;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets
{
    public class GetIdPacket : INetworkPacket
    {
        public ushort Id => 4;

        public ushort MyId { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(MyId);
        }

        public void Deserialize(BinaryReader r)
        {
            MyId = r.ReadUInt16();
        }
    }
}