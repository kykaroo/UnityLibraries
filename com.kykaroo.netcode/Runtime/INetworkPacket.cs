using System.IO;

namespace NetcodePackage.Runtime
{
    public interface INetworkPacket
    {
        ushort Id { get; }
        void Serialize(BinaryWriter w);
        void Deserialize(BinaryReader r);
    }
}