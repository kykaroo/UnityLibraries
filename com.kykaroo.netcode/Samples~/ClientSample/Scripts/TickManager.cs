using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace NetcodePackage.Samples.Example.Scripts
{
    public class TickManager
    {
        public ulong CurrentTick { get; private set; }
        public ulong ServerTick { get; private set; }

        private readonly Stopwatch _tickTimer = new();

        private double _tickInterval;
        private readonly double _baseTickInterval;
        public bool IsRunning { get; set; }

        public event Action<ulong> OnTick = delegate { };
        public event Action OnInputQueueClear = delegate { };

        public TickManager(int tickRate)
        {
            _baseTickInterval = 1000D / tickRate;
            _tickInterval = _baseTickInterval;
        }

        public async UniTaskVoid RunLoop()
        {
            _tickTimer.Start();
        
            double accumulatedTime = 0;
            var lastTime = _tickTimer.Elapsed.TotalMilliseconds;
        
            while (true)
            {
                var now = _tickTimer.Elapsed.TotalMilliseconds;
                var delta = now - lastTime;
                lastTime = now;
                accumulatedTime += delta;
        
                while (accumulatedTime >= _tickInterval)
                {
                    CurrentTick++;
                    OnTick(CurrentTick);
                    accumulatedTime -= _tickInterval;
                }

                await UniTask.Delay(1);
            }
        }

        public long SyncWithServer(ulong serverTick)
        {
            ServerTick = serverTick;

            var diff = (short)(CurrentTick - ServerTick);

            if (Math.Abs(diff) <= 2) return diff;

            const double adjustmentStepMs = 0.2;

            var stepMs = Math.Clamp(adjustmentStepMs * diff, -10, 10);
            _tickInterval = _baseTickInterval + stepMs;

            if (Math.Abs(diff) <= 10) return diff;

            HardResetTick(diff);

            return diff;
        }

        public void HardResetTick(short diff)
        {
            CurrentTick = ServerTick;
            OnInputQueueClear();
            Debug.Log($"[TickSync] Hard resync to {ServerTick} (TickDiff={diff})");
        }
    }
}