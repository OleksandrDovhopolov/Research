using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Infrastructure
{
    public sealed class AllowAllConditionProvider : IConditionProvider
    {
        public UniTask<bool> CanStartAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(true);
        }

        public UniTask<bool> ShouldForceEndAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(false);
        }
    }
}
