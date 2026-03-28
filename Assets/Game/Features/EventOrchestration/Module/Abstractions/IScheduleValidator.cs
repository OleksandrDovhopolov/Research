using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IScheduleValidator
    {
        UniTask<IReadOnlyList<string>> ValidateAsync(IReadOnlyList<ScheduleItem> items, CancellationToken ct);
    }
}
