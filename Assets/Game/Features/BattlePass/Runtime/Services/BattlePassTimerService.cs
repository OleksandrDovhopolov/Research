using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public sealed class BattlePassTimerService : IBattlePassTimerService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

        private readonly IBattlePassRealtimeClock _realtimeClock;

        private CancellationTokenSource _sessionCts;
        private DateTimeOffset _serverTimeAtStartUtc;
        private DateTimeOffset _endAtUtc;
        private double _realtimeAtStart;
        private bool _isRunning;

        public BattlePassTimerService(IBattlePassRealtimeClock realtimeClock)
        {
            _realtimeClock = realtimeClock ?? throw new ArgumentNullException(nameof(realtimeClock));
        }

        public event Action<TimeSpan> OnTimerUpdated;

        public TimeSpan CurrentRemaining { get; private set; } = TimeSpan.Zero;

        public void Start(DateTimeOffset serverTimeUtc, DateTimeOffset endAtUtc)
        {
            Stop();

            _serverTimeAtStartUtc = serverTimeUtc;
            _endAtUtc = endAtUtc;
            _realtimeAtStart = _realtimeClock.RealtimeSinceStartup;
            _isRunning = true;
            _sessionCts = new CancellationTokenSource();

            UpdateNow();
            RunHeartbeatAsync(_sessionCts.Token).Forget();
        }

        public void Stop()
        {
            _isRunning = false;
            CurrentRemaining = TimeSpan.Zero;

            if (_sessionCts == null)
            {
                return;
            }

            _sessionCts.Cancel();
            _sessionCts.Dispose();
            _sessionCts = null;
        }

        public void UpdateNow()
        {
            if (!_isRunning)
            {
                CurrentRemaining = TimeSpan.Zero;
                return;
            }

            var elapsedSeconds = Math.Max(0d, _realtimeClock.RealtimeSinceStartup - _realtimeAtStart);
            var currentServerTimeUtc = _serverTimeAtStartUtc.AddSeconds(elapsedSeconds);
            var remaining = _endAtUtc - currentServerTimeUtc;

            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            CurrentRemaining = remaining;
            OnTimerUpdated?.Invoke(CurrentRemaining);
        }

        private async UniTaskVoid RunHeartbeatAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(TickInterval, cancellationToken: ct);
                    UpdateNow();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
