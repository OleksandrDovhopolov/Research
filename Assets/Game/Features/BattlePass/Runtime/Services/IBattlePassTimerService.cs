using System;

namespace BattlePass
{
    public interface IBattlePassTimerService
    {
        event Action<TimeSpan> OnTimerUpdated;

        TimeSpan CurrentRemaining { get; }

        void Start(DateTimeOffset serverTimeUtc, DateTimeOffset endAtUtc);
        void Stop();
        void UpdateNow();
    }
}
