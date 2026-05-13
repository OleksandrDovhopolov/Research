using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using UnityEngine;

namespace EventOrchestration
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public sealed class ServerSynchronizedClock : IClock, IServerTimeSyncTarget
    {
        private readonly IServerTimeSyncSource _serverTimeSyncSource;
        private readonly Func<double> _realtimeNowProvider;
        private readonly Func<DateTimeOffset> _fallbackUtcNowProvider;

        private DateTimeOffset _serverUtcAtSync;
        private double _realtimeAtSyncSeconds;

        public ServerSynchronizedClock()
            : this(null, () => Time.realtimeSinceStartupAsDouble, () => DateTimeOffset.UtcNow)
        {
        }

        public ServerSynchronizedClock(IServerTimeSyncSource serverTimeSyncSource)
            : this(serverTimeSyncSource, () => Time.realtimeSinceStartupAsDouble, () => DateTimeOffset.UtcNow)
        {
        }

        public ServerSynchronizedClock(
            IServerTimeSyncSource serverTimeSyncSource,
            Func<double> realtimeNowProvider,
            Func<DateTimeOffset> fallbackUtcNowProvider)
        {
            _serverTimeSyncSource = serverTimeSyncSource;
            _realtimeNowProvider = realtimeNowProvider ?? throw new ArgumentNullException(nameof(realtimeNowProvider));
            _fallbackUtcNowProvider = fallbackUtcNowProvider ?? throw new ArgumentNullException(nameof(fallbackUtcNowProvider));
        }

        public bool IsSynchronized { get; private set; }

        public DateTimeOffset UtcNow
        {
            get
            {
                if (!IsSynchronized)
                {
                    return _fallbackUtcNowProvider();
                }

                var elapsedSeconds = Math.Max(0d, _realtimeNowProvider() - _realtimeAtSyncSeconds);
                return _serverUtcAtSync.AddSeconds(elapsedSeconds);
            }
        }

        public UniTask InitializeAsync(CancellationToken ct)
        {
            return RefreshAsync(ct);
        }

        public async UniTask RefreshAsync(CancellationToken ct)
        {
            if (_serverTimeSyncSource == null)
            {
                return;
            }

            ct.ThrowIfCancellationRequested();
            var serverUtcNow = await _serverTimeSyncSource.GetServerUtcNowAsync(ct);
            UpdateServerUtcNow(serverUtcNow);
        }

        public void UpdateServerUtcNow(DateTimeOffset serverUtcNow)
        {
            _serverUtcAtSync = serverUtcNow;
            _realtimeAtSyncSeconds = _realtimeNowProvider();
            IsSynchronized = true;
        }
    }
}
