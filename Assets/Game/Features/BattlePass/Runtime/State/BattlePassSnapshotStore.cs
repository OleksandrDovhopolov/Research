using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;

namespace BattlePass
{
    public sealed class BattlePassSnapshotStore : IBattlePassSnapshotStore, IDisposable
    {
        private readonly IBattlePassServerService _battlePassServerService;
        private readonly IBattlePassRealtimeClock _realtimeClock;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);

        private BattlePassSnapshot _currentSnapshot;
        private string _activePlayerId;
        private bool _isInitialized;
        private bool _isDisposed;

        public BattlePassSnapshotStore(
            IBattlePassServerService battlePassServerService,
            IBattlePassRealtimeClock realtimeClock,
            IPlayerIdentityProvider playerIdentityProvider)
        {
            _battlePassServerService = battlePassServerService ?? throw new ArgumentNullException(nameof(battlePassServerService));
            _realtimeClock = realtimeClock ?? throw new ArgumentNullException(nameof(realtimeClock));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }

        public event Action<BattlePassSnapshot> SnapshotChanged;

        public bool IsInitialized
        {
            get
            {
                ResetForPlayerChangeIfNeeded();
                return _isInitialized;
            }
        }

        public bool HasSnapshot
        {
            get
            {
                ResetForPlayerChangeIfNeeded();
                return _currentSnapshot != null;
            }
        }

        public bool LastSyncFailed { get; private set; }
        public BattlePassSnapshot CurrentSnapshot
        {
            get
            {
                ResetForPlayerChangeIfNeeded();
                return _currentSnapshot;
            }
        }

        public DateTimeOffset LastSyncUtc { get; private set; }
        public DateTimeOffset LastOpenRefreshUtc { get; private set; }

        public bool IsStale(DateTimeOffset nowUtc)
        {
            ResetForPlayerChangeIfNeeded();
            if (_currentSnapshot == null)
            {
                return true;
            }

            if (LastSyncUtc == default)
            {
                return true;
            }

            if (nowUtc < LastSyncUtc)
            {
                return false;
            }

            if (_currentSnapshot.Season?.EndAtUtc <= nowUtc)
            {
                return true;
            }

            return nowUtc - LastSyncUtc >= BattlePassConfig.Cache.SnapshotTtl;
        }

        public async UniTask RefreshAsync(CancellationToken ct, bool force = false)
        {
            ThrowIfDisposed();
            ResetForPlayerChangeIfNeeded();

            var nowUtc = _realtimeClock.UtcNow;
            if (!force)
            {
                LastOpenRefreshUtc = nowUtc;
                if (!IsStale(nowUtc))
                {
                    return;
                }
            }

            await _refreshSemaphore.WaitAsync(ct);
            try
            {
                ThrowIfDisposed();
                ResetForPlayerChangeIfNeeded();

                nowUtc = _realtimeClock.UtcNow;
                if (!force)
                {
                    LastOpenRefreshUtc = nowUtc;
                    if (!IsStale(nowUtc))
                    {
                        return;
                    }
                }

                var snapshot = await _battlePassServerService.GetCurrentAsync(ct);
                ct.ThrowIfCancellationRequested();
                ReplaceSnapshot(snapshot);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                LastSyncFailed = true;
                throw;
            }
            finally
            {
                _refreshSemaphore.Release();
            }
        }

        public void ReplaceSnapshot(BattlePassSnapshot snapshot)
        {
            ThrowIfDisposed();

            var currentPlayerId = _playerIdentityProvider.GetPlayerId();
            _activePlayerId = string.IsNullOrWhiteSpace(currentPlayerId) ? null : currentPlayerId;
            _currentSnapshot = snapshot;
            _isInitialized = true;
            LastSyncUtc = _realtimeClock.UtcNow;
            LastSyncFailed = false;

            SnapshotChanged?.Invoke(_currentSnapshot);
        }

        public bool TryApplyUserState(BattlePassUserState updatedUserState)
        {
            ThrowIfDisposed();
            ResetForPlayerChangeIfNeeded();

            if (updatedUserState == null || _currentSnapshot == null)
            {
                return false;
            }

            var mergedSnapshot = new BattlePassSnapshot(
                _currentSnapshot.Season,
                _currentSnapshot.Products,
                updatedUserState,
                _currentSnapshot.Levels,
                _currentSnapshot.ServerTimeUtc);

            ReplaceSnapshot(mergedSnapshot);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _refreshSemaphore.Dispose();
        }

        private void ResetForPlayerChangeIfNeeded()
        {
            var currentPlayerId = _playerIdentityProvider.GetPlayerId();
            if (string.Equals(_activePlayerId, currentPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            _activePlayerId = string.IsNullOrWhiteSpace(currentPlayerId) ? null : currentPlayerId;
            _currentSnapshot = null;
            _isInitialized = false;
            LastSyncUtc = default;
            LastOpenRefreshUtc = default;
            LastSyncFailed = false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(BattlePassSnapshotStore));
            }
        }
    }
}
