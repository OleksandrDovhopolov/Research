using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IEventController
    {
        string EventType { get; }

        UniTask InitializeAsync(ScheduleItem config, EventStateData state, CancellationToken ct);
        UniTask OnStart(CancellationToken ct);
        UniTask OnUpdate(CancellationToken ct);
        UniTask OnEnd(CancellationToken ct);
        UniTask ExecuteSettlement(CancellationToken ct);
    }
}
