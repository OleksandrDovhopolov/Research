using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IOrchestratorTelemetry
    {
        UniTask TrackTransitionAsync(string scheduleItemId, EventInstanceState from, EventInstanceState to, CancellationToken ct);
        UniTask TrackStartRejectedAsync(string scheduleItemId, string reason, CancellationToken ct);
        UniTask TrackFailureAsync(string scheduleItemId, string stage, Exception ex, CancellationToken ct);
    }
}
