using System.IO;
using NetcodePackage.Runtime;

namespace NetcodePackage.Samples.Example.Scripts.Packets
{
    public class PlayerInputPacket : INetworkPacket
    {
        public ushort Id => 8;
        public ulong Tick { get; set; }
        public ushort PlayerId { get; set; }
        public float InputX { get; set; }
        public float InputY { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(Tick);
            w.Write(PlayerId);
            w.Write(InputX);
            w.Write(InputY);
        }

        public void Deserialize(BinaryReader r)
        {
            Tick = r.ReadUInt64();
            PlayerId = r.ReadUInt16();
            InputX = r.ReadSingle();
            InputY = r.ReadSingle();
        }
    }
}