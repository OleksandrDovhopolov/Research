using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace EventOrchestration.Abstractions
{
    public interface IEventLifecycleEngine
    {
        event Action<ScheduleItem> OnEventStarting;
        event Action<ScheduleItem> OnEventStarted;
        event Action<ScheduleItem> OnEventCompleted;

        void BindStates(Dictionary<string, EventStateData> states);
        UniTask StartEventAsync(ScheduleItem item, CancellationToken ct);
        UniTask ProcessActiveEventAsync(ScheduleItem item, DateTimeOffset now, CancellationToken ct);
        void CleanupItem(string itemId);
    }
}
