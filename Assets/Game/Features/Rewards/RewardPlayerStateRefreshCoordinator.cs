using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public interface IRewardPlayerStateRefreshCoordinator
    {
        bool HasPendingRefresh { get; }
        void RequestBackgroundRefresh();
        UniTask RequestForegroundRefreshAsync(CancellationToken ct = default);
    }

    public sealed class RewardPlayerStateRefreshCoordinator : IRewardPlayerStateRefreshCoordinator, IDisposable
    {
        private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
        private readonly IRewardPlayerStateSyncService _rewardPlayerStateSyncService;

        private int _completedRevision;
        private int _requestedRevision;
        private bool _isDisposed;

        public RewardPlayerStateRefreshCoordinator(IRewardPlayerStateSyncService rewardPlayerStateSyncService)
        {
            _rewardPlayerStateSyncService = rewardPlayerStateSyncService ?? throw new ArgumentNullException(nameof(rewardPlayerStateSyncService));
        }

        public bool HasPendingRefresh => Volatile.Read(ref _completedRevision) < Volatile.Read(ref _requestedRevision);

        public void RequestBackgroundRefresh()
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _requestedRevision);
            RunBackgroundRefreshAsync().Forget();
        }

        public UniTask RequestForegroundRefreshAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _requestedRevision);
            return RefreshUntilCurrentAsync(ct, isBackground: false);
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

        private async UniTask RefreshUntilCurrentAsync(CancellationToken ct, bool isBackground)
        {
            while (HasPendingRefresh)
            {
                ct.ThrowIfCancellationRequested();
                await _refreshSemaphore.WaitAsync(ct);
                try
                {
                    var targetRevision = Volatile.Read(ref _requestedRevision);
                    if (Volatile.Read(ref _completedRevision) >= targetRevision)
                    {
                        continue;
                    }

                    try
                    {
                        await _rewardPlayerStateSyncService.SyncFromGlobalSaveAsync(ct);
                        Interlocked.Exchange(ref _completedRevision, targetRevision);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"[RewardPlayerStateRefreshCoordinator] {(isBackground ? "Background" : "Foreground")} refresh failed. {exception}");
                        return;
                    }
                }
                finally
                {
                    _refreshSemaphore.Release();
                }
            }
        }

        private async UniTaskVoid RunBackgroundRefreshAsync()
        {
            try
            {
                await RefreshUntilCurrentAsync(CancellationToken.None, isBackground: true);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(RewardPlayerStateRefreshCoordinator));
            }
        }
    }
}
