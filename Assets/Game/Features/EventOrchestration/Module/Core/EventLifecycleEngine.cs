using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration
{
    public sealed class EventLifecycleEngine : IEventLifecycleEngine
    {
        private readonly IEventRegistry _eventRegistry;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IOrchestratorTelemetry _telemetry;

        private readonly HashSet<string> _transitionInFlight = new();
        private readonly HashSet<string> _hydratedControllers = new();
        private Dictionary<string, EventStateData> _states;

        public event Action<ScheduleItem> OnEventStarting;
        public event Action<ScheduleItem> OnEventStarted;
        public event Action<ScheduleItem> OnEventCompleted;

        public EventLifecycleEngine(
            IEventRegistry eventRegistry,
            IClock clock,
            IStateStore stateStore,
            IOrchestratorTelemetry telemetry)
        {
            _eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public void BindStates(Dictionary<string, EventStateData> states)
        {
            _states = states ?? throw new ArgumentNullException(nameof(states));
            _hydratedControllers.Clear();
        }

        public async UniTask StartEventAsync(ScheduleItem item, CancellationToken ct)
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
                await _stateStore.SaveAsync(_states, ct);

                await EnsureControllerHydratedAsync(item, state, controller, ct);

                OnEventStarting?.Invoke(item);

                if (!state.StartInvoked)
                {
                    await controller.OnStart(ct);
                }

                if (!state.StartInvoked)
                {
                    state.StartInvoked = true;
                }

                await TransitionStateAsync(state, EventInstanceState.Active, ct);
                await _stateStore.SaveAsync(_states, ct);

                OnEventStarted?.Invoke(item);

                await ProcessActiveEventAsync(item, _clock.UtcNow, ct);
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

        public async UniTask ProcessActiveEventAsync(ScheduleItem item, DateTimeOffset now, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!_eventRegistry.TryGet(item.EventType, out var controller))
            {
                throw new InvalidOperationException($"No controller for event type '{item.EventType}'.");
            }

            var state = _states[item.Id];
            try
            {
                await EnsureControllerHydratedAsync(item, state, controller, ct);

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

        public void CleanupItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            _transitionInFlight.Remove(itemId);
            _hydratedControllers.Remove(itemId);
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
            _hydratedControllers.Remove(item.Id);
            RemoveItemFromRuntime(item.Id);
            await _stateStore.SaveAsync(_states, ct);
        }

        private async UniTask EnsureControllerHydratedAsync(
            ScheduleItem item,
            EventStateData state,
            IEventController controller,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (_hydratedControllers.Contains(item.Id))
            {
                return;
            }

            await controller.InitializeAsync(item, state, ct);

            if (state.StartInvoked &&
                state.State is EventInstanceState.Active
                    or EventInstanceState.Starting
                    or EventInstanceState.Ending
                    or EventInstanceState.Settling)
            {
                await controller.OnStart(ct);
            }

            _hydratedControllers.Add(item.Id);
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
            _hydratedControllers.Remove(itemId);
        }

        private void RemoveItemFromRuntime(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            _states.Remove(itemId);
            _transitionInFlight.Remove(itemId);
            _hydratedControllers.Remove(itemId);
        }
    }
}
