using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Infrastructure;

namespace EventOrchestration
{
    public sealed class InMemoryStateStore : IStateStore
    {
        private readonly SaveService _saveService;

        public InMemoryStateStore(SaveService saveService)
        {
            _saveService = saveService;
        }

        public async UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var savedStates = await _saveService.GetReadonlyModuleAsync(data => data.EventStates
                .Select(x => new EventStateSaveData
                {
                    ScheduleItemId = x.ScheduleItemId,
                    State = x.State,
                    Version = x.Version,
                    UpdatedAtUnixSeconds = x.UpdatedAtUnixSeconds,
                    LastError = x.LastError,
                    StartInvoked = x.StartInvoked,
                    EndInvoked = x.EndInvoked,
                    SettlementInvoked = x.SettlementInvoked,
                })
                .ToList(), ct);
            var result = new Dictionary<string, EventStateData>(savedStates.Count);
            foreach (var state in savedStates)
            {
                if (string.IsNullOrWhiteSpace(state.ScheduleItemId))
                {
                    continue;
                }

                result[state.ScheduleItemId] = new EventStateData
                {
                    ScheduleItemId = state.ScheduleItemId,
                    State = (EventInstanceState)state.State,
                    Version = state.Version,
                    UpdatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(state.UpdatedAtUnixSeconds),
                    LastError = state.LastError,
                    StartInvoked = state.StartInvoked,
                    EndInvoked = state.EndInvoked,
                    SettlementInvoked = state.SettlementInvoked,
                };
            }

            return result;
        }

        public async UniTask SaveAsync(Dictionary<string, EventStateData> states, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var saveStates = states?.Values.Select(x => new EventStateSaveData
            {
                ScheduleItemId = x.ScheduleItemId,
                State = (int)x.State,
                Version = x.Version,
                UpdatedAtUnixSeconds = x.UpdatedAtUtc.ToUnixTimeSeconds(),
                LastError = x.LastError,
                StartInvoked = x.StartInvoked,
                EndInvoked = x.EndInvoked,
                SettlementInvoked = x.SettlementInvoked,
            }).ToList() ?? new List<EventStateSaveData>();

            await _saveService.UpdateModuleAsync(data => data.EventStates, eventStates =>
            {
                eventStates.Clear();
                eventStates.AddRange(saveStates);
            }, ct);
        }
    }
}
