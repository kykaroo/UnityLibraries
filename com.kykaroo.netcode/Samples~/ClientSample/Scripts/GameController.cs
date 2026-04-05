using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Data;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Objects;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Packets;
using com.kykaroo.netcode.Samples.ClientSample.Scripts.Views;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts
{
    public class GameController
    {
        private readonly Player _playerPrefab;
        private readonly GameClient _gameClient;
        private readonly TickManager _tickManager;
        private readonly GameView _gameView;

        private ushort _myId;
        private Player _myObject;
        private Player _myServerObject;

        private const float PlayerSpeed = 0.2f;

        private readonly Queue<PlayerInputState> _pendingInputs = new();

        private readonly Dictionary<ushort, Player> _players = new();
        private Vector2 _lastServerPosition;
        private ulong _lastConfirmedTick;
        private Vector2 _predictedPosition;

        private readonly SynchronizationContext _unityContext = SynchronizationContext.Current;

        public GameController(Player playerPrefab, GameClient gameClient, TickManager tickManager, GameView gameView)
        {
            _playerPrefab = playerPrefab;
            _gameClient = gameClient;
            _tickManager = tickManager;
            _gameView = gameView;

            _tickManager.OnTick += OnTick;
            _tickManager.OnInputQueueClear += () => _pendingInputs.Clear();

            _gameClient.OnConnected += () => _tickManager.IsRunning = true;
            _gameClient.OnDisconnected += () => _tickManager.IsRunning = false;

            _gameClient.OnPing += ping =>
            {
                var d = Math.Round(ping, 2);
                var round = (float)d;
                var text = round.ToString(CultureInfo.InvariantCulture);

                SetInMainThread(_ => { _gameView.PingValueText.text = text; });
            };

            _gameView.TargetFpsInputField.onValueChanged.AddListener(value =>
            {
                var targetFps = int.Parse(value);
                _gameView.TargetFpsSlider.SetValueWithoutNotify(targetFps);

                Application.targetFrameRate = targetFps;
            });

            _gameView.TargetFpsSlider.onValueChanged.AddListener(value =>
            {
                var targetFps = (int)value;
                _gameView.TargetFpsInputField.SetTextWithoutNotify(targetFps.ToString());

                Application.targetFrameRate = targetFps;
            });

            const int targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            _gameView.TargetFpsSlider.SetValueWithoutNotify(targetFrameRate);
            _gameView.TargetFpsInputField.SetTextWithoutNotify(targetFrameRate.ToString());
            _ = _tickManager.RunLoop();
        }

        public void SetInMainThread(Action<object> action)
        {
            _unityContext.Post(action.Invoke, null);
        }

        public Player AddPlayer(ushort playerId, Vector2 pos)
        {
            if (_players.TryGetValue(playerId, out var existingPlayer))
            {
                existingPlayer.transform.position = pos;

                return existingPlayer;
            }

            var player = Object.Instantiate(_playerPrefab);
            player.transform.position = pos;
            player.PlayerData = new PlayerData(playerId);

            _players.Add(playerId, player);

            return player;
        }

        public void SetMyId(ushort myId)
        {
            _myId = myId;

            _myObject = AddPlayer(myId, Vector2.zero);

            if (_myServerObject == null)
            {
                _myServerObject = Object.Instantiate(_playerPrefab);
                _myServerObject.transform.position = _myObject.transform.position;
                _myServerObject.SpriteRenderer.color = new Color32(255, 0, 0, 64);
            }

            _gameView.PlayerIdValueText.text = myId.ToString();
        }

        public void RemovePlayer(ushort playerId)
        {
            if (_players.Remove(playerId, out var player))
            {
                Object.Destroy(player.gameObject);
            }
        }

        public void ServerMovePlayer(ulong serverTick, ushort playerId, Vector2 serverPosition)
        {
            if (playerId == _myId)
            {
                _lastServerPosition = serverPosition;
                _lastConfirmedTick = serverTick;

                _myServerObject.gameObject.transform.position = _lastServerPosition;

                _predictedPosition = serverPosition;

                while (_pendingInputs.Count > 0 && _pendingInputs.Peek().Tick <= serverTick)
                {
                    _pendingInputs.Dequeue();
                }

                foreach (var input in _pendingInputs)
                {
                    _predictedPosition += input.MoveDir * PlayerSpeed;
                }

                _myObject.PlayerData.PosX = _predictedPosition.x;
                _myObject.PlayerData.PosY = _predictedPosition.y;

                _myObject.transform.position = Vector2.Lerp(_myObject.transform.position, _predictedPosition, 0.5f);

                return;
            }

            if (_players.TryGetValue(playerId, out var player) == false) return;

            player.transform.position = serverPosition;
        }

        private void HandleInput(ulong tick)
        {
            if (_myObject == null) return;

            var moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            var moveDelta = moveDirection * PlayerSpeed;

            _myObject.PlayerData.PosX += moveDelta.x;
            _myObject.PlayerData.PosY += moveDelta.y;
            _myObject.transform.position = new Vector3(_myObject.PlayerData.PosX, _myObject.PlayerData.PosY, 0);

            if (moveDirection.magnitude == 0) return;

            _pendingInputs.Enqueue(new PlayerInputState
            {
                Tick = tick,
                MoveDir = moveDirection
            });

            _gameClient.EnqueueReliable(new PlayerInputPacket
            {
                Tick = tick,
                PlayerId = _myId,
                InputX = moveDirection.x,
                InputY = moveDirection.y
            });
        }

        private void OnTick(ulong tick)
        {
            HandleInput(tick);

            _gameClient.FlushReliable();
        }

        public void SyncTick(ulong tick)
        {
            var diff = _tickManager.SyncWithServer(tick);

            SetInMainThread(_ =>
            {
                _gameView.TickDiffValueText.text = diff.ToString();
                _gameView.ServerTickValueText.text = _tickManager.ServerTick.ToString();
                _gameView.ClientTickValueText.text = _tickManager.CurrentTick.ToString();
                _gameView.FpsValueText.text = Mathf.RoundToInt(1 / Time.unscaledDeltaTime).ToString();
            });
        }

        public void Ping()
        {
            _gameClient.ProcessPing();
        }

        public void SyncWorld(WorldDataPacket packet)
        {
            _tickManager.HardResetTick(0);

            SetInMainThread(_ =>
            {
                foreach (var playerData in packet.Players)
                {
                    AddPlayer(playerData.PlayerId, new Vector2(playerData.PosX, playerData.PosY));
                }
            });
        }
    }
}