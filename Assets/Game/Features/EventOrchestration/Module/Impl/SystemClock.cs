using System;
using EventOrchestration.Abstractions;

namespace EventOrchestration
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
