using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Infrastructure
{
    public sealed class InMemoryScheduleProvider : IScheduleProvider
    {
        private readonly IReadOnlyList<ScheduleItem> _schedule;

        public InMemoryScheduleProvider(IReadOnlyList<ScheduleItem> schedule)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }

        public UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(_schedule);
        }
    }
}
