using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IStateStore
    {
        UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct);
        UniTask SaveAsync(Dictionary<string, EventStateData> states, CancellationToken ct);
    }
}
