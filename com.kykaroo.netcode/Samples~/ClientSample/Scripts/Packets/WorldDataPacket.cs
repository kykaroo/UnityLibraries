using System.IO;
using NetcodePackage.Runtime;
using NetcodePackage.Samples.Example.Scripts.Data;

namespace NetcodePackage.Samples.Example.Scripts.Packets
{
    public class WorldDataPacket : INetworkPacket
    {
        public ushort Id => 7;

        private ushort _playerCount;
        public ulong ServerTick { get; set; }
        public PlayerData[] Players { get; set; }

        public void Serialize(BinaryWriter w)
        {
            w.Write(_playerCount);
            w.Write(ServerTick);

            foreach (var player in Players)
            {
                w.Write(player.PlayerId);
                w.Write(player.PosX);
                w.Write(player.PosY);
            }
        }

        public void Deserialize(BinaryReader r)
        {
            _playerCount = r.ReadUInt16();
            ServerTick = r.ReadUInt64();

            Players = new PlayerData[_playerCount];

            for (var i = 0; i < _playerCount; i++)
            {
                var playerId = r.ReadUInt16();
                var player = Players[i] = new PlayerData(playerId);
                player.PosX = r.ReadSingle();
                player.PosY = r.ReadSingle();
            }
        }
    }
}