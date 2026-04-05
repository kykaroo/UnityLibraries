using System.IO;
using Netcode;

namespace ClientSample
{
    public class PlayerLeavePacket : INetworkPacket
    {
        public ushort Id => 2;

        public ushort PlayerId { get; private set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(PlayerId);
        }

        public void Deserialize(BinaryReader r)
        {
            PlayerId = r.ReadUInt16();
        }
    }
}