using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassGameplayReadyInitializerTests
    {
        [Test]
        public void InitializeAsync_RefreshesSnapshotStore()
        {
            var snapshotStore = new StubBattlePassSnapshotStore();
            var initializer = new BattlePassGameplayReadyInitializer(snapshotStore);

            initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotStore.RefreshCalls, Is.EqualTo(1));
            Assert.That(snapshotStore.LastForceValue, Is.True);
        }

        [Test]
        public void InitializeAsync_WhenRefreshFails_DoesNotThrow()
        {
            var snapshotStore = new StubBattlePassSnapshotStore
            {
                RefreshAsyncFactory = (_, _) => ThrowStubFailureAsync()
            };
            var initializer = new BattlePassGameplayReadyInitializer(snapshotStore);

            Assert.DoesNotThrow(() => initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult());
            Assert.That(snapshotStore.RefreshCalls, Is.EqualTo(1));
        }

        private static async UniTask ThrowStubFailureAsync()
        {
            await UniTask.Yield();
            throw new InvalidOperationException("stub failure");
        }

        private sealed class StubBattlePassSnapshotStore : IBattlePassSnapshotStore
        {
            public event Action<BattlePassSnapshot> SnapshotChanged;

            public Func<CancellationToken, bool, UniTask> RefreshAsyncFactory { get; set; }
            public int RefreshCalls { get; private set; }
            public bool LastForceValue { get; private set; }

            public bool IsInitialized => false;
            public bool HasSnapshot => false;
            public bool LastSyncFailed => false;
            public BattlePassSnapshot CurrentSnapshot => null;
            public DateTimeOffset LastSyncUtc => default;
            public DateTimeOffset LastOpenRefreshUtc => default;

            public bool IsStale(DateTimeOffset nowUtc)
            {
                return true;
            }

            public UniTask RefreshAsync(CancellationToken ct, bool force = false)
            {
                RefreshCalls++;
                LastForceValue = force;
                return RefreshAsyncFactory?.Invoke(ct, force) ?? UniTask.CompletedTask;
            }

            public void ReplaceSnapshot(BattlePassSnapshot snapshot)
            {
                SnapshotChanged?.Invoke(snapshot);
            }

            public bool TryApplyUserState(BattlePassUserState updatedUserState)
            {
                return false;
            }
        }
    }
}
