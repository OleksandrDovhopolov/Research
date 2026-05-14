using System;

namespace BattlePass
{
    public interface IBattlePassRealtimeClock
    {
        DateTimeOffset UtcNow { get; }
        double RealtimeSinceStartup { get; }
    }
}
