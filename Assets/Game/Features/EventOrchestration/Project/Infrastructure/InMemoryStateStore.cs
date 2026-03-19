using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Infrastructure
{
    public sealed class InMemoryStateStore : IStateStore
    {
        private Dictionary<string, EventStateData> _storage = new();

        public UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var copy = new Dictionary<string, EventStateData>(_storage);
            return UniTask.FromResult(copy);
        }

        public UniTask SaveAsync(Dictionary<string, EventStateData> states, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _storage = new Dictionary<string, EventStateData>(states);
            return UniTask.CompletedTask;
        }
    }
}
