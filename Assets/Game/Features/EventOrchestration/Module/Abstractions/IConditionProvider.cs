using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IConditionProvider
    {
        UniTask<bool> CanStartAsync(ScheduleItem item, CancellationToken ct);
        UniTask<bool> ShouldForceEndAsync(ScheduleItem item, CancellationToken ct);
    }
}
