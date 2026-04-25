using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EventOrchestration.Abstractions
{
    public interface IServerTimeSyncSource
    {
        UniTask<DateTimeOffset> GetServerUtcNowAsync(CancellationToken ct);
    }

    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
