using System.IO;
using com.kykaroo.netcode.Runtime;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets
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