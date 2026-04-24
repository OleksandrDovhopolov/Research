using System;
using NUnit.Framework;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassTimerServiceTests
    {
        [Test]
        public void UpdateNow_UsesServerTimeBaseline_InsteadOfLocalClock()
        {
            var realtimeClock = new FakeRealtimeClock();
            var timerService = new BattlePassTimerService(realtimeClock);

            timerService.Start(
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
                DateTimeOffset.Parse("2026-04-24T10:00:10Z"));

            Assert.That(timerService.CurrentRemaining.TotalSeconds, Is.EqualTo(10d).Within(0.01d));

            realtimeClock.Advance(3.25d);
            timerService.UpdateNow();

            Assert.That(timerService.CurrentRemaining.TotalSeconds, Is.EqualTo(6.75d).Within(0.01d));
            timerService.Stop();
        }

        [Test]
        public void UpdateNow_ClampsRemainingTimeToZero()
        {
            var realtimeClock = new FakeRealtimeClock();
            var timerService = new BattlePassTimerService(realtimeClock);

            timerService.Start(
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
                DateTimeOffset.Parse("2026-04-24T10:00:05Z"));

            realtimeClock.Advance(12d);
            timerService.UpdateNow();

            Assert.That(timerService.CurrentRemaining, Is.EqualTo(TimeSpan.Zero));
            timerService.Stop();
        }

        private sealed class FakeRealtimeClock : IBattlePassRealtimeClock
        {
            public double RealtimeSinceStartup { get; private set; }

            public void Advance(double seconds)
            {
                RealtimeSinceStartup += seconds;
            }
        }
    }
}
