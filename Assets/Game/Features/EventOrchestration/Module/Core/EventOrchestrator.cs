using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace EventOrchestration.Core
{
    public sealed class EventOrchestrator
    {
        private const string DebugLogPath = @"c:\Projects\Research\.cursor\debug.log";
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IEventRegistry _eventRegistry;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IOrchestratorTelemetry _telemetry;

        private readonly Dictionary<string, EventStateData> _states = new();
        private readonly HashSet<string> _transitionInFlight = new();
        private readonly HashSet<string> _hydratedControllers = new();
        private readonly UniTaskCompletionSource _initializedTcs = new();
        private List<ScheduleItem> _schedule = new();
        private bool _isInitialized;

        public event Action<ScheduleItem> OnEventCreated;
        public event Action<ScheduleItem> OnEventStarted;
        public event Action<ScheduleItem> OnEventCompleted;

        public bool IsInitialized => _isInitialized;

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
            #region agent log
            Debug.LogWarning("[Debug] EventOrchestrator.InitializeAsync entered");
            WriteDebugLog("H1", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync entered", new
            {
                isCancellationRequested = ct.IsCancellationRequested
            });
            #endregion
            ct.ThrowIfCancellationRequested();
            try
            {
                #region agent log
                Debug.LogWarning("[Debug] EventOrchestrator.InitializeAsync loading schedule");
                WriteDebugLog("H3", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync loading schedule");
                #endregion
                var loadedSchedule = await _scheduleProvider.LoadAsync(ct);
                #region agent log
                Debug.LogWarning($"[Debug] EventOrchestrator.InitializeAsync loaded schedule count={loadedSchedule?.Count ?? -1}");
                WriteDebugLog("H3", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync loaded schedule", new
                {
                    count = loadedSchedule?.Count ?? -1
                });
                #endregion
                
                var validationErrors = await _scheduleValidator.ValidateAsync(loadedSchedule, ct);
                #region agent log
                Debug.LogWarning($"[Debug] EventOrchestrator.InitializeAsync validation errors count={validationErrors.Count}");
                WriteDebugLog("H4", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync validation errors", new
                {
                    count = validationErrors.Count
                });
                #endregion
                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
                }
                
                #region agent log
                Debug.LogWarning("[Debug] EventOrchestrator.InitializeAsync building upcoming schedule");
                WriteDebugLog("H5", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync building upcoming schedule");
                #endregion
                _schedule = loadedSchedule
                    .OrderBy(x => x.StreamId)
                    .ThenByDescending(x => x.Priority)
                    .ThenBy(x => x.StartTimeUtc)
                    .ToList();
                #region agent log
                Debug.LogWarning($"[Debug] EventOrchestrator.InitializeAsync built upcoming schedule count={_schedule.Count}");
                WriteDebugLog("H5", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync built upcoming schedule", new
                {
                    count = _schedule.Count
                });
                #endregion

                foreach (var scheduleItem in _schedule)
                {
                    Debug.LogWarning($"[Debug] scheduleItem {scheduleItem.Id}, {scheduleItem.StartTimeUtc}, {scheduleItem.EndTimeUtc}" );
                }
                
                var restored = await _stateStore.LoadAsync(ct);
                _states.Clear();
                _hydratedControllers.Clear();
                foreach (var pair in restored)
                {
                    _states[pair.Key] = pair.Value;
                }
                
                _schedule = BuildRuntimeSchedule(_schedule);

                CreateScheduleData();

                await _stateStore.SaveAsync(_states, ct);

                if (!_isInitialized)
                {
                    _isInitialized = true;
                    _initializedTcs.TrySetResult();
                }
            }
            catch (OperationCanceledException)
            {
                #region agent log
                Debug.LogWarning("[Debug] EventOrchestrator.InitializeAsync cancelled");
                WriteDebugLog("H2", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync cancelled");
                #endregion
                throw;
            }
            catch (Exception ex)
            {
                #region agent log
                Debug.LogError($"[Debug] EventOrchestrator.InitializeAsync failed: {ex}");
                WriteDebugLog("H3", "EventOrchestrator.InitializeAsync", "[Debug] EventOrchestrator.InitializeAsync failed", new
                {
                    exceptionType = ex.GetType().FullName,
                    ex.Message
                });
                #endregion
                _initializedTcs.TrySetException(ex);
                throw;
            }
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
        
        public ScheduleItem GetNextUpcomingEvent()
        {
            var now = _clock.UtcNow;
            ScheduleItem nextItem = null;
            for (var i = 0; i < _schedule.Count; i++)
            {
                var item = _schedule[i];
                if (item.EndTimeUtc <= now)
                    continue;

                if (!_states.TryGetValue(item.Id, out var state))
                    continue;

                if (state.State != EventInstanceState.Pending)
                    continue;

                if (nextItem == null || item.StartTimeUtc < nextItem.StartTimeUtc)
                {
                    nextItem = item;
                }
            }

            return nextItem;
        }

        public IReadOnlyList<ScheduleItem> GetScheduleSnapshot()
        {
            return _schedule.ToList();
        }

        public bool TryGetStateSnapshot(string scheduleItemId, out EventStateData stateSnapshot)
        {
            if (string.IsNullOrWhiteSpace(scheduleItemId))
            {
                stateSnapshot = null;
                return false;
            }

            if (!_states.TryGetValue(scheduleItemId, out var state))
            {
                stateSnapshot = null;
                return false;
            }

            stateSnapshot = new EventStateData
            {
                ScheduleItemId = state.ScheduleItemId,
                State = state.State,
                Version = state.Version,
                UpdatedAtUtc = state.UpdatedAtUtc,
                LastError = state.LastError,
                StartInvoked = state.StartInvoked,
                EndInvoked = state.EndInvoked,
                SettlementInvoked = state.SettlementInvoked,
            };
            return true;
        }

        public UniTask WaitUntilInitializedAsync(CancellationToken ct)
        {
            if (_isInitialized)
            {
                return UniTask.CompletedTask;
            }

            ct.ThrowIfCancellationRequested();
            return _initializedTcs.Task.AttachExternalCancellation(ct);
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

        private List<ScheduleItem> BuildRuntimeSchedule(IEnumerable<ScheduleItem> schedule)
        {
            var now = _clock.UtcNow;
            return schedule
                .Where(item => ShouldKeepForRuntime(item, now))
                .OrderBy(x => x.StreamId)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.StartTimeUtc)
                .ToList();
        }

        private bool ShouldKeepForRuntime(ScheduleItem item, DateTimeOffset now)
        {
            if (item.EndTimeUtc > now)
            {
                return true;
            }

            if (!_states.TryGetValue(item.Id, out var state) || state == null)
            {
                return false;
            }

            if (state.State is EventInstanceState.Active
                or EventInstanceState.Starting
                or EventInstanceState.Ending
                or EventInstanceState.Settling)
            {
                return true;
            }

            if (state.StartInvoked && !state.EndInvoked)
            {
                return true;
            }

            if (state.EndInvoked && !state.SettlementInvoked)
            {
                return true;
            }

            return false;
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

                    var activeState = _states[active.Id].State;
                    if (activeState is EventInstanceState.Completed
                        or EventInstanceState.Failed
                        or EventInstanceState.Cancelled)
                    {
                        continue;
                    }

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

                await EnsureControllerHydratedAsync(item, state, controller, ct);
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
            _hydratedControllers.Remove(item.Id);
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

        #region Debug
        
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
            
            _schedule = BuildRuntimeSchedule(updatedSchedule);

            CreateScheduleData();

            await _stateStore.SaveAsync(_states, ct);
        }
        
        public async UniTask<bool> DebugCompleteCurrentEventAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var now = _clock.UtcNow;

            var current = _schedule.FirstOrDefault(item =>
            {
                if (!_states.TryGetValue(item.Id, out var state)) return false;

                return state.State 
                    is EventInstanceState.Active 
                    or EventInstanceState.Starting 
                    or EventInstanceState.Ending 
                    or EventInstanceState.Settling;
            });

            if (current == null)
            {
                return false;
            }

            if (current.EndTimeUtc > now)
            {
                current.EndTimeUtc = now;
            }

            await TickAsync(ct);
            await _stateStore.SaveAsync(_states, ct);
            return true;
        }

        #endregion

        private static void WriteDebugLog(string hypothesisId, string location, string message, object data = null)
        {
            try
            {
                var payload = new
                {
                    runId = "initial",
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
                File.AppendAllText(DebugLogPath, JsonConvert.SerializeObject(payload) + Environment.NewLine);
            }
            catch
            {
                // Instrumentation must never break runtime flow.
            }
        }
    }
}
