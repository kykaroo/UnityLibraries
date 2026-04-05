using System.IO;
using Netcode;

namespace ClientSample
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