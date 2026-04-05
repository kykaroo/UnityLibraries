using System.IO;
using Netcode;

namespace ClientSample
{
    public class TickPacket : INetworkPacket
    {
        public ushort Id => 6;

        public ulong Tick { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(Tick);
        }

        public void Deserialize(BinaryReader r)
        {
            Tick = r.ReadUInt64();
        }
    }
}