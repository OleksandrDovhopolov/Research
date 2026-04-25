using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface ILiveOpsScheduleContentSource
    {
        UniTask<string> LoadJsonAsync(CancellationToken ct);
    }

    public interface IScheduleProvider
    {
        UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct);
    }
}
