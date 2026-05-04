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

    public sealed class ServerSynchronizedClock : IClock
    {
        private readonly IServerTimeSyncSource _serverTimeSyncSource;
        private readonly Func<double> _realtimeNowProvider;
        private readonly Func<DateTimeOffset> _fallbackUtcNowProvider;

        private DateTimeOffset _serverUtcAtSync;
        private double _realtimeAtSyncSeconds;

        public ServerSynchronizedClock(IServerTimeSyncSource serverTimeSyncSource)
            : this(serverTimeSyncSource, () => Time.realtimeSinceStartupAsDouble, () => DateTimeOffset.UtcNow)
        {
        }

        public ServerSynchronizedClock(
            IServerTimeSyncSource serverTimeSyncSource,
            Func<double> realtimeNowProvider,
            Func<DateTimeOffset> fallbackUtcNowProvider)
        {
            _serverTimeSyncSource = serverTimeSyncSource ?? throw new ArgumentNullException(nameof(serverTimeSyncSource));
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
            ct.ThrowIfCancellationRequested();
            _serverUtcAtSync = await _serverTimeSyncSource.GetServerUtcNowAsync(ct);
            _realtimeAtSyncSeconds = _realtimeNowProvider();
            IsSynchronized = true;
        }
    }
}
