using System;

namespace EventOrchestration.Abstractions
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
