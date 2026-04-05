using System.IO;

namespace Netcode
{
    public interface INetworkPacket
    {
        ushort Id { get; }
        void Serialize(BinaryWriter w);
        void Deserialize(BinaryReader r);
    }
}