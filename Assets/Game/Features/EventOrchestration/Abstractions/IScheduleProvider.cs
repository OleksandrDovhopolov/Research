using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IScheduleProvider
    {
        UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct);
    }
}
