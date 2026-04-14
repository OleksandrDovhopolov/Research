using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration
{
    internal sealed class EventOrchestratorDebugFacade
    {
        private readonly EventOrchestrator _orchestrator;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;

        public EventOrchestratorDebugFacade(
            EventOrchestrator orchestrator,
            IScheduleValidator scheduleValidator,
            IClock clock,
            IStateStore stateStore)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _scheduleValidator = scheduleValidator ?? throw new ArgumentNullException(nameof(scheduleValidator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        }

        public async UniTask AddScheduleItemForDebugAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var updatedSchedule = _orchestrator.MutableSchedule.ToList();
            updatedSchedule.Add(item);

            var validationErrors = await _scheduleValidator.ValidateAsync(updatedSchedule, ct);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
            }

            _orchestrator.SetSchedule(_orchestrator.RebuildRuntimeSchedule(updatedSchedule));
            _orchestrator.RunCreateScheduleData();

            await _stateStore.SaveAsync(_orchestrator.MutableStates, ct);
        }

        public async UniTask<bool> DebugCompleteCurrentEventAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var now = _clock.UtcNow;
            var current = FindInProgressEvent();
            var hasCurrent = current != null;

            if (hasCurrent && current.EndTimeUtc > now)
            {
                current.EndTimeUtc = now;
            }

            await _orchestrator.TickAsync(ct);
            _orchestrator.RunPruneStatesOutsideSchedule();
            await _stateStore.SaveAsync(_orchestrator.MutableStates, ct);
            return hasCurrent;
        }

        public async UniTask<bool> ForceNextEventAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var now = _clock.UtcNow;

            var active = FindInProgressEvent();
            var targetStreamId = active?.StreamId;

            if (active != null)
            {
                if (active.EndTimeUtc > now)
                {
                    active.EndTimeUtc = now;
                }

                await _orchestrator.TickAsync(ct);
                await _stateStore.SaveAsync(_orchestrator.MutableStates, ct);

                if (IsInProgress(active.Id))
                {
                    return false;
                }

                now = _clock.UtcNow;
            }

            var next = FindNextPendingEvent(now, targetStreamId);
            if (next == null)
            {
                return false;
            }

            if (next.StartTimeUtc > now)
            {
                next.StartTimeUtc = now;
            }

            if (next.EndTimeUtc <= now)
            {
                next.EndTimeUtc = now.AddMinutes(1);
            }

            await _orchestrator.TickAsync(ct);
            await _stateStore.SaveAsync(_orchestrator.MutableStates, ct);

            return _orchestrator.MutableStates.TryGetValue(next.Id, out var state) &&
                   state.State is EventInstanceState.Active or EventInstanceState.Starting;
        }

        private ScheduleItem FindInProgressEvent()
        {
            var schedule = _orchestrator.MutableSchedule;
            for (var i = 0; i < schedule.Count; i++)
            {
                var item = schedule[i];
                if (IsInProgress(item.Id))
                {
                    return item;
                }
            }

            return null;
        }

        private bool IsInProgress(string scheduleItemId)
        {
            if (!_orchestrator.MutableStates.TryGetValue(scheduleItemId, out var state))
            {
                return false;
            }

            return state.State is EventInstanceState.Active or EventInstanceState.Starting or EventInstanceState.Ending
                or EventInstanceState.Settling;
        }

        private ScheduleItem FindNextPendingEvent(DateTimeOffset now, string streamId)
        {
            var schedule = _orchestrator.MutableSchedule;
            var states = _orchestrator.MutableStates;
            for (var i = 0; i < schedule.Count; i++)
            {
                var item = schedule[i];

                if (!string.IsNullOrEmpty(streamId) && item.StreamId != streamId)
                {
                    continue;
                }

                if (!states.TryGetValue(item.Id, out var state))
                {
                    continue;
                }

                if (state.State != EventInstanceState.Pending)
                {
                    continue;
                }

                if (item.EndTimeUtc <= now)
                {
                    continue;
                }

                return item;
            }

            return null;
        }
    }
}
