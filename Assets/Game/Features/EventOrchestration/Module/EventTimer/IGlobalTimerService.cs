using System;

namespace UIShared
{
    public interface IGlobalTimerService
    {
        event Action<string, TimeSpan> OnTick;
        event Action<string> OnTimerFinished;

        void Register(string eventId, DateTimeOffset endTimeUtc);
        void Unregister(string eventId);
        bool TryGetRemaining(string eventId, out TimeSpan remaining);
    }
}
