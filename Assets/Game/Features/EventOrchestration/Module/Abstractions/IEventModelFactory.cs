using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IEventModelFactory
    {
        UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct);
    }
}
