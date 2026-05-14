using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassSnapshotStoreTests
    {
        [Test]
        public void ReplaceSnapshot_SetsStateAndClearsFailure()
        {
            var store = CreateStore(out _, out var clock, out _);
            var snapshot = CreateSnapshot(xp: 100);
            clock.UtcNow = DateTimeOffset.Parse("2026-04-24T10:00:00Z");

            store.ReplaceSnapshot(snapshot);

            Assert.That(store.IsInitialized, Is.True);
            Assert.That(store.HasSnapshot, Is.True);
            Assert.That(store.CurrentSnapshot, Is.SameAs(snapshot));
            Assert.That(store.LastSyncFailed, Is.False);
            Assert.That(store.LastSyncUtc, Is.EqualTo(clock.UtcNow));
        }

        [Test]
        public void TryApplyUserState_MergesUserStateWithoutDroppingStaticSnapshotData()
        {
            var store = CreateStore(out _, out _, out _);
            var snapshot = CreateSnapshot(xp: 100);
            store.ReplaceSnapshot(snapshot);

            var updatedUserState = new BattlePassUserState(
                "season_1",
                4,
                250,
                BattlePassPassType.Platinum,
                Array.Empty<BattlePassClaimedRewardCell>(),
                Array.Empty<BattlePassClaimableRewardCell>());

            var result = store.TryApplyUserState(updatedUserState);

            Assert.That(result, Is.True);
            Assert.That(store.CurrentSnapshot.UserState, Is.SameAs(updatedUserState));
            Assert.That(store.CurrentSnapshot.Season, Is.SameAs(snapshot.Season));
            Assert.That(store.CurrentSnapshot.Products, Is.SameAs(snapshot.Products));
            Assert.That(store.CurrentSnapshot.Levels, Is.SameAs(snapshot.Levels));
        }

        [Test]
        public void IsStale_ReturnsTrue_WhenSnapshotMissingOrExpired()
        {
            var store = CreateStore(out _, out var clock, out _);

            Assert.That(store.IsStale(clock.UtcNow), Is.True);

            store.ReplaceSnapshot(CreateSnapshot(xp: 100));
            Assert.That(store.IsStale(clock.UtcNow), Is.False);

            clock.UtcNow += BattlePassConfig.Cache.SnapshotTtl + TimeSpan.FromSeconds(1);
            Assert.That(store.IsStale(clock.UtcNow), Is.True);
        }

        [Test]
        public void RefreshAsync_SerializesParallelRequests()
        {
            var store = CreateStore(out var service, out _, out _);
            var completionSource = new UniTaskCompletionSource<BattlePassSnapshot>();
            service.GetCurrentAsyncFactory = _ => completionSource.Task;

            var firstRefresh = store.RefreshAsync(CancellationToken.None, force: true);
            var secondRefresh = store.RefreshAsync(CancellationToken.None, force: true);

            Assert.That(service.GetCurrentCalls, Is.EqualTo(1));

            completionSource.TrySetResult(CreateSnapshot(xp: 200));

            firstRefresh.GetAwaiter().GetResult();
            secondRefresh.GetAwaiter().GetResult();

            Assert.That(service.GetCurrentCalls, Is.EqualTo(2));
            Assert.That(store.HasSnapshot, Is.True);
        }

        [Test]
        public void RefreshAsync_ResetsSnapshot_WhenPlayerChanges()
        {
            var store = CreateStore(out var service, out var clock, out var identityProvider);
            store.ReplaceSnapshot(CreateSnapshot(xp: 100));

            identityProvider.PlayerId = "player_2";
            clock.UtcNow += TimeSpan.FromSeconds(1);
            service.GetCurrentAsyncFactory = _ => UniTask.FromResult(CreateSnapshot(xp: 300));

            store.RefreshAsync(CancellationToken.None, force: true).GetAwaiter().GetResult();

            Assert.That(store.CurrentSnapshot.UserState.Xp, Is.EqualTo(300));
            Assert.That(store.IsInitialized, Is.True);
        }

        private static BattlePassSnapshotStore CreateStore(
            out StubBattlePassServerService service,
            out FakeRealtimeClock clock,
            out StubPlayerIdentityProvider identityProvider)
        {
            service = new StubBattlePassServerService();
            clock = new FakeRealtimeClock(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
            identityProvider = new StubPlayerIdentityProvider();
            return new BattlePassSnapshotStore(service, clock, identityProvider);
        }

        private static BattlePassSnapshot CreateSnapshot(int xp)
        {
            return new BattlePassSnapshot(
                new BattlePassSeason(
                    "season_1",
                    "Season 1",
                    DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    50,
                    "active",
                    "v1"),
                new BattlePassProducts("premium_sku", "platinum_sku"),
                new BattlePassUserState(
                    "season_1",
                    3,
                    xp,
                    BattlePassPassType.Premium,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(1, 0, new BattlePassRewardRef("reward_default"), new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        }

        private sealed class StubBattlePassServerService : IBattlePassServerService
        {
            public Func<CancellationToken, UniTask<BattlePassSnapshot>> GetCurrentAsyncFactory { get; set; }
            public int GetCurrentCalls { get; private set; }

            public UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                GetCurrentCalls++;
                return GetCurrentAsyncFactory?.Invoke(ct) ?? UniTask.FromResult(CreateSnapshot(xp: 100));
            }

            public UniTask<BattlePassAddXpResult> AddXpAsync(int amount, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

            public UniTask<BattlePassClaimResult> ClaimAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct = default)
            {
                throw new NotImplementedException();
            }

        }

        private sealed class FakeRealtimeClock : IBattlePassRealtimeClock
        {
            public FakeRealtimeClock(DateTimeOffset utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTimeOffset UtcNow { get; set; }
            public double RealtimeSinceStartup => 0d;
        }

        private sealed class StubPlayerIdentityProvider : IBattlePassPlayerContext
        {
            public string PlayerId { get; set; } = "player_1";

            public string GetPlayerId()
            {
                return PlayerId;
            }
        }
    }
}
