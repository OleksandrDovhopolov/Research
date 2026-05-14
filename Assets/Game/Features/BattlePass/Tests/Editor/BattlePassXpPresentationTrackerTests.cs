using NUnit.Framework;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassXpPresentationTrackerTests
    {
        [Test]
        public void InitializeBaseline_SetsBaselineOnlyOnce_PerSeason()
        {
            var identityProvider = new StubPlayerIdentityProvider();
            var tracker = new BattlePassXpPresentationTracker(identityProvider);

            tracker.InitializeBaseline("season_1", 3, 100);
            tracker.InitializeBaseline("season_1", 4, 200);

            Assert.That(tracker.TryGetBaseline("season_1", out var level, out var xp), Is.True);
            Assert.That(level, Is.EqualTo(3));
            Assert.That(xp, Is.EqualTo(100));
        }

        [Test]
        public void CommitPresented_UpdatesBaseline_ForSeason()
        {
            var identityProvider = new StubPlayerIdentityProvider();
            var tracker = new BattlePassXpPresentationTracker(identityProvider);

            tracker.InitializeBaseline("season_1", 3, 100);
            tracker.CommitPresented("season_1", 5, 240);

            Assert.That(tracker.TryGetBaseline("season_1", out var level, out var xp), Is.True);
            Assert.That(level, Is.EqualTo(5));
            Assert.That(xp, Is.EqualTo(240));
        }

        [Test]
        public void Tracker_ResetsOnPlayerChange()
        {
            var identityProvider = new StubPlayerIdentityProvider();
            var tracker = new BattlePassXpPresentationTracker(identityProvider);

            tracker.InitializeBaseline("season_1", 3, 100);
            identityProvider.PlayerId = "player_2";

            Assert.That(tracker.TryGetBaseline("season_1", out _, out _), Is.False);
        }

        [Test]
        public void Tracker_KeepsIndependentBaselines_PerSeason()
        {
            var tracker = new BattlePassXpPresentationTracker(new StubPlayerIdentityProvider());

            tracker.InitializeBaseline("season_1", 3, 100);
            tracker.InitializeBaseline("season_2", 1, 0);

            Assert.That(tracker.TryGetBaseline("season_1", out var level1, out var xp1), Is.True);
            Assert.That(level1, Is.EqualTo(3));
            Assert.That(xp1, Is.EqualTo(100));
            Assert.That(tracker.TryGetBaseline("season_2", out var level2, out var xp2), Is.True);
            Assert.That(level2, Is.EqualTo(1));
            Assert.That(xp2, Is.EqualTo(0));
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
