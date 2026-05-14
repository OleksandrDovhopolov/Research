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
            var startupSync = new StubBattlePassStartupSync();
            var initializer = new BattlePassGameplayReadyInitializer(startupSync);

            initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(startupSync.InitializeCalls, Is.EqualTo(1));
        }

        [Test]
        public void InitializeAsync_WhenRefreshFails_DoesNotThrow()
        {
            var startupSync = new StubBattlePassStartupSync
            {
                InitializeAsyncFactory = _ => ThrowStubFailureAsync()
            };
            var initializer = new BattlePassGameplayReadyInitializer(startupSync);

            Assert.DoesNotThrow(() => initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult());
            Assert.That(startupSync.InitializeCalls, Is.EqualTo(1));
        }

        private static async UniTask ThrowStubFailureAsync()
        {
            await UniTask.Yield();
            throw new InvalidOperationException("stub failure");
        }

        private sealed class StubBattlePassStartupSync : IBattlePassStartupSync
        {
            public Func<CancellationToken, UniTask> InitializeAsyncFactory { get; set; }
            public int InitializeCalls { get; private set; }

            public UniTask InitializeAsync(CancellationToken ct)
            {
                InitializeCalls++;
                return InitializeAsyncFactory?.Invoke(ct) ?? UniTask.CompletedTask;
            }
        }
    }
}
