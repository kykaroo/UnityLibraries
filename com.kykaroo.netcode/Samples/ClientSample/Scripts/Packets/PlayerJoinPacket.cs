using System.IO;
using com.kykaroo.netcode.Runtime;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets
{
    public class PlayerJoinPacket : INetworkPacket
    {
        public ushort Id => 1;
        
        public ushort PlayerId { get; set; }
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