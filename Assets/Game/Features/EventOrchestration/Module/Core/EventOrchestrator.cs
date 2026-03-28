using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Core
{
    public sealed class EventOrchestrator
    {
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IEventRegistry _eventRegistry;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IOrchestratorTelemetry _telemetry;

        private readonly Dictionary<string, EventStateData> _states = new();
        private readonly HashSet<string> _transitionInFlight = new();
        private List<ScheduleItem> _schedule = new();

        public event Action<ScheduleItem> OnEventCreated;
        public event Action<ScheduleItem> OnEventStarted;
        public event Action<ScheduleItem> OnEventCompleted;

        public EventOrchestrator(
            IScheduleProvider scheduleProvider,
            IScheduleValidator scheduleValidator,
            IEventRegistry eventRegistry,
            IClock clock,
            IStateStore stateStore,
            IOrchestratorTelemetry telemetry)
        {
            _scheduleProvider = scheduleProvider ?? throw new ArgumentNullException(nameof(scheduleProvider));
            _scheduleValidator = scheduleValidator ?? throw new ArgumentNullException(nameof(scheduleValidator));
            _eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var loadedSchedule = await _scheduleProvider.LoadAsync(ct);
            var validationErrors = await _scheduleValidator.ValidateAsync(loadedSchedule, ct);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
            }
            
            _schedule = BuildUpcomingSchedule(loadedSchedule);
            
            var restored = await _stateStore.LoadAsync(ct);
            _states.Clear();
            foreach (var pair in restored)
            {
                _states[pair.Key] = pair.Value;
            }

            CreateScheduleData();

            await _stateStore.SaveAsync(_states, ct);
        }

        public async UniTask TickAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var now = _clock.UtcNow;
            var groupedByStream = _schedule.GroupBy(x => x.StreamId);
            foreach (var stream in groupedByStream)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessStreamAsync(stream, now, ct);
            }
        }

        public async UniTask AddScheduleItemForDebugAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var updatedSchedule = _schedule.ToList();
            updatedSchedule.Add(item);

            var validationErrors = await _scheduleValidator.ValidateAsync(updatedSchedule, ct);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
            }
            
            _schedule = BuildUpcomingSchedule(updatedSchedule);

            CreateScheduleData();

            await _stateStore.SaveAsync(_states, ct);
        }

        private void CreateScheduleData()
        {
            var now = _clock.UtcNow;
            foreach (var item in _schedule)
            {
                if (_states.ContainsKey(item.Id))
                    continue;

                _states[item.Id] = new EventStateData
                {
                    ScheduleItemId = item.Id,
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                };

                OnEventCreated?.Invoke(item);
            }
        }

        private List<ScheduleItem> BuildUpcomingSchedule(IEnumerable<ScheduleItem> schedule)
        {
            var now = _clock.UtcNow;
            return schedule
                .Where(x => x.EndTimeUtc > now)
                .OrderBy(x => x.StreamId)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.StartTimeUtc)
                .ToList();
        }
        
        private async UniTask ProcessStreamAsync(IEnumerable<ScheduleItem> streamItems, DateTimeOffset now, CancellationToken ct)
        {
            var items = streamItems as IList<ScheduleItem> ?? streamItems.ToList();
            var safetyCounter = items.Count + 1;

            // Loop enables "instant chain": next valid event can start in the same tick.
            while (safetyCounter-- > 0)
            {
                ct.ThrowIfCancellationRequested();

                var active = FindActive(items);
                if (active != null)
                {
                    await ProcessActiveAsync(active, now, ct);
                    return;
                }

                var candidate = FindStartCandidateAsync(items, now);
                if (candidate == null)
                {
                    return;
                }

                await StartAsync(candidate, ct);

                var state = _states[candidate.Id];
                if (state.State != EventInstanceState.Completed)
                {
                    return;
                }
            }
        }

        private ScheduleItem FindActive(IList<ScheduleItem> streamItems)
        {
            for (var i = 0; i < streamItems.Count; i++)
            {
                var state = _states[streamItems[i].Id].State;
                if (state == EventInstanceState.Active ||
                    state == EventInstanceState.Starting ||
                    state == EventInstanceState.Ending ||
                    state == EventInstanceState.Settling)
                {
                    return streamItems[i];
                }
            }

            return null;
        }

        private static readonly TimeSpan StartSkew = TimeSpan.FromSeconds(0);
        
        private ScheduleItem FindStartCandidateAsync(IList<ScheduleItem> streamItems, DateTimeOffset now)
        {
            for (var i = 0; i < streamItems.Count; i++)
            {
                var item = streamItems[i];
                var state = _states[item.Id].State;
                if (state != EventInstanceState.Pending)
                    continue;

                var startGate = item.StartTimeUtc - StartSkew;
                if (startGate > now || now >= item.EndTimeUtc)
                    continue;

                return item;
            }

            return null;
        }

        private async UniTask StartAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!_eventRegistry.TryGet(item.EventType, out var controller))
            {
                throw new InvalidOperationException($"No controller for event type '{item.EventType}'.");
            }

            if (!_transitionInFlight.Add(item.Id))
                return;

            try
            {
                var state = _states[item.Id];
                await TransitionStateAsync(state, EventInstanceState.Starting, ct);

                await controller.InitializeAsync(item, state, ct);

                if (!state.StartInvoked)
                {
                    await controller.OnStart(ct);
                    state.StartInvoked = true;
                }

                await TransitionStateAsync(state, EventInstanceState.Active, ct);
                OnEventStarted?.Invoke(item);

                await ProcessActiveAsync(item, _clock.UtcNow, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await MarkFailedAsync(item.Id, "Start", ex, ct);
            }
            finally
            {
                _transitionInFlight.Remove(item.Id);
            }
        }

        private async UniTask ProcessActiveAsync(ScheduleItem item, DateTimeOffset now, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!_eventRegistry.TryGet(item.EventType, out var controller))
            {
                throw new InvalidOperationException($"No controller for event type '{item.EventType}'.");
            }

            var state = _states[item.Id];
            try
            {
                if (state.State == EventInstanceState.Active)
                {
                    await controller.OnUpdate(ct);
                }

                var shouldEndByTime = now >= item.EndTimeUtc;

                if (shouldEndByTime)
                {
                    await EndAndSettleAsync(item, controller, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await MarkFailedAsync(item.Id, "UpdateOrEnd", ex, ct);
            }
        }

        private async UniTask EndAndSettleAsync(ScheduleItem item, IEventController controller, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var state = _states[item.Id];
            await TransitionStateAsync(state, EventInstanceState.Ending, ct);

            if (!state.EndInvoked)
            {
                await controller.OnEnd(ct);
                state.EndInvoked = true;
            }

            await TransitionStateAsync(state, EventInstanceState.Settling, ct);

            if (!state.SettlementInvoked)
            {
                await controller.ExecuteSettlement(ct);
                state.SettlementInvoked = true;
            }

            await TransitionStateAsync(state, EventInstanceState.Completed, ct);
            OnEventCompleted?.Invoke(item);
            await _stateStore.SaveAsync(_states, ct);
        }

        private async UniTask TransitionStateAsync(EventStateData state, EventInstanceState to, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var from = state.State;
            state.State = to;
            state.UpdatedAtUtc = _clock.UtcNow;
            state.Version++;

            await _telemetry.TrackTransitionAsync(state.ScheduleItemId, from, to, ct);
        }

        private async UniTask MarkFailedAsync(string itemId, string stage, Exception exception, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var state = _states[itemId];
            var from = state.State;
            state.State = EventInstanceState.Failed;
            state.LastError = $"{stage}: {exception.Message}";
            state.UpdatedAtUtc = _clock.UtcNow;
            state.Version++;

            await _telemetry.TrackTransitionAsync(itemId, from, EventInstanceState.Failed, ct);
            await _telemetry.TrackFailureAsync(itemId, stage, exception, ct);
            await _stateStore.SaveAsync(_states, ct);
        }
        
    }
}
