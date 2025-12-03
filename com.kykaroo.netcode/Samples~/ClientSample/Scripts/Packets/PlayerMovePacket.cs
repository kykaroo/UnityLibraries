using System.IO;
using NetcodePackage.Runtime;

namespace NetcodePackage.Samples.Example.Scripts.Packets
{
    public class PlayerMovePacket : INetworkPacket
    {
        public ushort Id => 3;
        public ulong Tick { get; private set; }

        public ushort PlayerId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(Tick);
            w.Write(PlayerId);

            w.Write(X);
            w.Write(Y);
        }

        public void Deserialize(BinaryReader r)
        {
            Tick = r.ReadUInt64();
            PlayerId = r.ReadUInt16();
            X = r.ReadSingle();
            Y = r.ReadSingle();
        }
    }
}