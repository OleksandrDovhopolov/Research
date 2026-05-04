using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rewards.Tests.Editor
{
    public sealed class RewardPlayerStateRefreshCoordinatorTests
    {
        [Test]
        public void RequestForegroundRefreshAsync_ClearsDirtyFlag_WhenSyncSucceeds()
        {
            var syncService = new StubRewardPlayerStateSyncService();
            var coordinator = new RewardPlayerStateRefreshCoordinator(syncService);

            coordinator.RequestForegroundRefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(syncService.CallCount, Is.EqualTo(1));
            Assert.That(coordinator.HasPendingRefresh, Is.False);
        }

        [Test]
        public void RequestBackgroundRefresh_KeepsDirtyFlag_WhenSyncFails()
        {
            var syncService = new StubRewardPlayerStateSyncService
            {
                ExceptionToThrow = new InvalidOperationException("sync failed")
            };
            var coordinator = new RewardPlayerStateRefreshCoordinator(syncService);

            LogAssert.Expect(LogType.Error, new Regex("\\[RewardPlayerStateRefreshCoordinator\\] Background refresh failed\\."));
            coordinator.RequestBackgroundRefresh();
            WaitUntilAsync(() => syncService.CallCount > 0).GetAwaiter().GetResult();

            Assert.That(coordinator.HasPendingRefresh, Is.True);
        }

        [Test]
        public void RequestForegroundRefreshAsync_SerializesConcurrentRefreshes()
        {
            var firstCallBlocker = new UniTaskCompletionSource();
            var syncService = new StubRewardPlayerStateSyncService
            {
                FirstCallBlocker = firstCallBlocker
            };
            var coordinator = new RewardPlayerStateRefreshCoordinator(syncService);

            var firstRefresh = coordinator.RequestForegroundRefreshAsync(CancellationToken.None);
            WaitUntilAsync(() => syncService.CallCount == 1 && syncService.CurrentConcurrentCalls == 1).GetAwaiter().GetResult();
            var secondRefresh = coordinator.RequestForegroundRefreshAsync(CancellationToken.None);

            firstCallBlocker.TrySetResult();
            UniTask.WhenAll(firstRefresh, secondRefresh).GetAwaiter().GetResult();

            Assert.That(syncService.CallCount, Is.EqualTo(2));
            Assert.That(syncService.MaxConcurrentCalls, Is.EqualTo(1));
            Assert.That(coordinator.HasPendingRefresh, Is.False);
        }

        private static async UniTask WaitUntilAsync(Func<bool> predicate)
        {
            for (var i = 0; i < 20; i++)
            {
                if (predicate())
                {
                    return;
                }

                await UniTask.Delay(10);
            }

            Assert.Fail("Condition was not met in time.");
        }

        private sealed class StubRewardPlayerStateSyncService : IRewardPlayerStateSyncService
        {
            public Exception ExceptionToThrow { get; set; }
            public UniTaskCompletionSource FirstCallBlocker { get; set; }
            public int CallCount { get; private set; }
            public int CurrentConcurrentCalls { get; private set; }
            public int MaxConcurrentCalls { get; private set; }

            public async UniTask SyncFromGlobalSaveAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                CallCount++;
                CurrentConcurrentCalls++;
                MaxConcurrentCalls = Math.Max(MaxConcurrentCalls, CurrentConcurrentCalls);

                try
                {
                    if (CallCount == 1 && FirstCallBlocker != null)
                    {
                        await FirstCallBlocker.Task.AttachExternalCancellation(ct);
                    }

                    if (ExceptionToThrow != null)
                    {
                        throw ExceptionToThrow;
                    }
                }
                finally
                {
                    CurrentConcurrentCalls--;
                }
            }
        }
    }
}
