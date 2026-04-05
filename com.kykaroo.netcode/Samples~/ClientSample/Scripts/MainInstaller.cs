using System;
using System.Collections.Generic;
using com.kykaroo.netcode.Runtime;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.MainMenu;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Objects;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Views;
using UnityEngine;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts
{
    public class MainInstaller : MonoBehaviour
    {
        [SerializeField] private MainMenuView mainMenuView = default!;
        [SerializeField] private Player playerPrefab = default!;
        [SerializeField] private GameView gameView = default!;

        private GameController _gameController;

        public void Awake()
        {
            var packets = new List<(Type, Delegate)>

            {
                (typeof(PlayerJoinPacket), (Action<PlayerJoinPacket>)OnPlayerJoin),
                (typeof(PlayerLeavePacket), (Action<PlayerLeavePacket>)OnPlayerLeave),
                (typeof(PlayerMovePacket), (Action<PlayerMovePacket>)OnPlayerMove),
                (typeof(GetIdPacket), (Action<GetIdPacket>)OnGetId),
                (typeof(PingPacket), (Action<PingPacket>)OnPing),
                (typeof(TickPacket), (Action<TickPacket>)OnTick),
                (typeof(WorldDataPacket), (Action<WorldDataPacket>)OnWorldData),
                (typeof(PlayerInputPacket), (Action<PlayerInputPacket>)OnPlayerInput),
            };

            var packetRegistry = new PacketRegistry();
            var gameClient = new GameClient(packetRegistry);
            var mainMenuPresenter = new MainMenuPresenter(mainMenuView, gameClient);

            const int tickRate = 30;

            var tickManager = new TickManager(tickRate);
            _gameController = new GameController(playerPrefab, gameClient, tickManager, gameView);

            var registryInitializer = new ClientPacketRegistryInitializer(packetRegistry, packets);

            registryInitializer.RegisterAll();
        }


        private void OnPlayerInput(INetworkPacket packet)
        {
            if (packet is not PlayerInputPacket playerInputPacket)
            {
                throw new Exception();
            }
        }

        private void OnWorldData(WorldDataPacket packet)
        {
            if (packet is not WorldDataPacket worldDataPacket)
            {
                throw new Exception();
            }

            _gameController.SyncWorld(worldDataPacket);
            Debug.Log($"[Packet] World data: players - {worldDataPacket.Players.Length}");
        }

        private void OnTick(INetworkPacket packet)
        {
            if (packet is not TickPacket tickPacket)
            {
                throw new Exception();
            }

            _gameController.SyncTick(tickPacket.Tick);
        }

        private void OnPing(INetworkPacket packet)
        {
            if (packet is not PingPacket pingPacket)
            {
                throw new Exception();
            }

            _gameController.Ping();
        }

        private void OnGetId(INetworkPacket packet)
        {
            if (packet is not GetIdPacket getIdPacket)
            {
                throw new Exception();
            }

            _gameController.SetInMainThread(_ => { _gameController.SetMyId(getIdPacket.MyId); });
            Debug.Log($"[Packet] My id is: {getIdPacket.MyId}");
        }

        private void OnPlayerJoin(INetworkPacket packet)
        {
            if (packet is not PlayerJoinPacket playerJoinPacket)
            {
                throw new Exception();
            }

            _gameController.SetInMainThread(
                _ => { _gameController.AddPlayer(playerJoinPacket.PlayerId, Vector2.zero); });

            Debug.Log($"[Packet] Player joined: {playerJoinPacket.PlayerId}");
        }

        private void OnPlayerLeave(INetworkPacket packet)
        {
            if (packet is not PlayerLeavePacket playerLeavePacket)
            {
                throw new Exception();
            }

            _gameController.SetInMainThread(_ => { _gameController.RemovePlayer(playerLeavePacket.PlayerId); });
            Debug.Log($"[Packet] Player left: {playerLeavePacket.PlayerId}");
        }

        private void OnPlayerMove(INetworkPacket packet)
        {
            if (packet is not PlayerMovePacket playerMovePacket)
            {
                throw new Exception();
            }

            _gameController.SetInMainThread(_ =>
            {
                _gameController.ServerMovePlayer(playerMovePacket.Tick, playerMovePacket.PlayerId,
                    new Vector2(playerMovePacket.X, playerMovePacket.Y));
            });
        }
    }
}