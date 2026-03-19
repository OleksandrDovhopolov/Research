using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Core
{
    public sealed class BasicScheduleValidator : IScheduleValidator
    {
        public UniTask<IReadOnlyList<string>> ValidateAsync(IReadOnlyList<ScheduleItem> items, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var errors = new List<string>();
            if (items == null)
            {
                errors.Add("Schedule list is null.");
                return UniTask.FromResult((IReadOnlyList<string>)errors);
            }

            var ids = new HashSet<string>();
            for (var i = 0; i < items.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var item = items[i];
                if (item == null)
                {
                    errors.Add($"Item at index {i} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Id))
                    errors.Add($"Item at index {i} has empty Id.");
                else if (!ids.Add(item.Id))
                    errors.Add($"Duplicate schedule Id: {item.Id}.");

                if (string.IsNullOrWhiteSpace(item.EventType))
                    errors.Add($"Item {item.Id} has empty EventType.");

                if (item.EndTimeUtc <= item.StartTimeUtc)
                    errors.Add($"Item {item.Id} has invalid time window.");
            }

            return UniTask.FromResult((IReadOnlyList<string>)errors);
        }
    }
}
