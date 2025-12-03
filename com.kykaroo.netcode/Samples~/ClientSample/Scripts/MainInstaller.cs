using System;
using System.Collections.Generic;
using NetcodePackage.Runtime;
using NetcodePackage.Samples.Example.Scripts.MainMenu;
using NetcodePackage.Samples.Example.Scripts.Objects;
using NetcodePackage.Samples.Example.Scripts.Packets;
using NetcodePackage.Samples.Example.Scripts.Views;
using UnityEngine;
using Zenject;

namespace NetcodePackage.Samples.Example.Scripts
{
    public class MainInstaller : MonoInstaller
    {
        [SerializeField] private MainMenuView mainMenuView = default!;
        [SerializeField] private Player player = default!;
        [SerializeField] private GameView gameView = default!;

        private GameController _gameController;

        public override void InstallBindings()
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

            Container.BindInterfacesAndSelfTo<MainMenuPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<PacketRegistry>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GameClient>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GameController>().AsSingle().NonLazy();

            const int l = 30;

            Container.BindInterfacesAndSelfTo<TickManager>().AsSingle().WithArguments(l).NonLazy();

            Container.BindInterfacesAndSelfTo<ClientPacketRegistryInitializer>().AsSingle().WithArguments(packets)
                .NonLazy();

            Container.BindInterfacesAndSelfTo<MainMenuView>().FromInstance(mainMenuView).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<Player>().FromInstance(player).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GameView>().FromInstance(gameView).AsSingle().NonLazy();

            var registryInitializer = Container.Resolve<ClientPacketRegistryInitializer>();

            _gameController = Container.Resolve<GameController>();

            registryInitializer.RegisterAll();
        }


        private void OnPlayerInput(PlayerInputPacket packet)
        {
            if (packet is not PlayerInputPacket playerInputPacket)
            {
                throw new Exception();
            }
        }

        private void OnWorldData(INetworkPacket packet)
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

            _gameController.SetMyId(getIdPacket.MyId);
            Debug.Log($"[Packet] My id is: {getIdPacket.MyId}");
        }

        private void OnPlayerJoin(INetworkPacket packet)
        {
            if (packet is not PlayerJoinPacket playerJoinPacket)
            {
                throw new Exception();
            }

            _gameController.AddPlayer(playerJoinPacket.PlayerId, Vector2.zero);
            Debug.Log($"[Packet] Player joined: {playerJoinPacket.PlayerId}");
        }

        private void OnPlayerLeave(INetworkPacket packet)
        {
            if (packet is not PlayerLeavePacket playerLeavePacket)
            {
                throw new Exception();
            }

            _gameController.RemovePlayer(playerLeavePacket.PlayerId);
            Debug.Log($"[Packet] Player left: {playerLeavePacket.PlayerId}");
        }

        private void OnPlayerMove(INetworkPacket packet)
        {
            if (packet is not PlayerMovePacket playerMovePacket)
            {
                throw new Exception();
            }

            _gameController.ServerMovePlayer(playerMovePacket.Tick, playerMovePacket.PlayerId,
                new Vector2(playerMovePacket.X, playerMovePacket.Y));
        }
    }
}