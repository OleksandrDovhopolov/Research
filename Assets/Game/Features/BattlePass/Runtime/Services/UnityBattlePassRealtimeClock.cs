using EventOrchestration.Abstractions;
using UnityEngine;

namespace BattlePass
{
    public sealed class UnityBattlePassRealtimeClock : IBattlePassRealtimeClock
    {
        private readonly IClock _clock;

        public UnityBattlePassRealtimeClock(IClock clock)
        {
            _clock = clock;
        }

        public System.DateTimeOffset UtcNow => _clock.UtcNow;
        public double RealtimeSinceStartup => Time.realtimeSinceStartupAsDouble;
    }
}
