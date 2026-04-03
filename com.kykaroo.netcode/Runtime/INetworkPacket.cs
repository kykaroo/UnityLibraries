using System.IO;

namespace com.kykaroo.netcode.Runtime
{
    public interface INetworkPacket
    {
        ushort Id { get; }
        void Serialize(BinaryWriter w);
        void Deserialize(BinaryReader r);
    }
}