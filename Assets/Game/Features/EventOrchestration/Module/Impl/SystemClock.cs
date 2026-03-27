using System;
using EventOrchestration.Abstractions;

namespace EventOrchestration.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
