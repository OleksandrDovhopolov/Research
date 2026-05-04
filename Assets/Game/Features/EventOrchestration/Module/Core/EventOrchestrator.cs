using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration
{
    public sealed class EventOrchestrator
    {
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IEventLifecycleEngine _engine;
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;

        private readonly Dictionary<string, EventStateData> _states = new();
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly UniTaskCompletionSource _initializedTcs = new();
        private List<ScheduleItem> _schedule = new();
        private bool _isInitialized;

        public event Action<ScheduleItem> OnEventCreated;
        public event Action<ScheduleItem> OnEventStarting;
        public event Action<ScheduleItem> OnEventStarted;
        public event Action<ScheduleItem> OnEventCompleted;

        public bool IsInitialized => _isInitialized;

        public EventOrchestrator(
            IScheduleProvider scheduleProvider,
            IScheduleValidator scheduleValidator,
            IClock clock,
            IStateStore stateStore,
            IEventLifecycleEngine engine)
        {
            _scheduleProvider = scheduleProvider ?? throw new ArgumentNullException(nameof(scheduleProvider));
            _scheduleValidator = scheduleValidator ?? throw new ArgumentNullException(nameof(scheduleValidator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            _engine.OnEventStarting += item => OnEventStarting?.Invoke(item);
            _engine.OnEventStarted += item => OnEventStarted?.Invoke(item);
            _engine.OnEventCompleted += item => OnEventCompleted?.Invoke(item);
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(ct);
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                var restored = await _stateStore.LoadAsync(ct);
                _states.Clear();
                foreach (var pair in restored)
                {
                    _states[pair.Key] = pair.Value;
                }

                _engine.BindStates(_states);

                await ReloadScheduleAsync(removeAllStaleStates: true, ct);

                _isInitialized = true;
                _initializedTcs.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _initializedTcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async UniTask TickAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(ct);
            try
            {
                var now = _clock.UtcNow;
                var groupedByStream = _schedule.GroupBy(x => x.StreamId);
                foreach (var stream in groupedByStream)
                {
                    ct.ThrowIfCancellationRequested();
                    await ProcessStreamAsync(stream, now, ct);
                }
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async UniTask RefreshScheduleAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(ct);
            try
            {
                if (!_isInitialized)
                {
                    throw new InvalidOperationException("EventOrchestrator must be initialized before refresh.");
                }

                await ReloadScheduleAsync(removeAllStaleStates: false, ct);
            }
            finally
            {
                _operationLock.Release();
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
                {
                    continue;
                }

                if (!_states.TryGetValue(item.Id, out var state))
                {
                    continue;
                }

                if (state.State != EventInstanceState.Pending)
                {
                    continue;
                }

                if (nextItem == null || item.StartTimeUtc < nextItem.StartTimeUtc)
                {
                    nextItem = item;
                }
            }

            return nextItem;
        }

        public bool IsEventTypeActive(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                return false;
            }

            for (var i = 0; i < _schedule.Count; i++)
            {
                var item = _schedule[i];
                if (!string.Equals(item.EventType, eventType, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_states.TryGetValue(item.Id, out var state))
                {
                    continue;
                }

                if (IsInProgressState(state.State))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsEventTypeUpcoming(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                return false;
            }

            var now = _clock.UtcNow;
            for (var i = 0; i < _schedule.Count; i++)
            {
                var item = _schedule[i];
                if (!string.Equals(item.EventType, eventType, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_states.TryGetValue(item.Id, out var state))
                {
                    continue;
                }

                if (state.State == EventInstanceState.Pending && item.StartTimeUtc > now && item.EndTimeUtc > now)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCurrentEvent(string eventType, out ScheduleItem item)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                item = null;
                return false;
            }

            for (var i = 0; i < _schedule.Count; i++)
            {
                var candidate = _schedule[i];
                if (!string.Equals(candidate.EventType, eventType, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_states.TryGetValue(candidate.Id, out var state))
                {
                    continue;
                }

                if (!IsInProgressState(state.State))
                {
                    continue;
                }

                item = candidate;
                return true;
            }

            item = null;
            return false;
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

        #region Internal accessors for debug facade

        internal IList<ScheduleItem> MutableSchedule => _schedule;
        internal Dictionary<string, EventStateData> MutableStates => _states;

        internal void SetSchedule(List<ScheduleItem> schedule) => _schedule = schedule;
        internal List<ScheduleItem> RebuildRuntimeSchedule(IEnumerable<ScheduleItem> s) => BuildRuntimeSchedule(s);
        internal void RunCreateScheduleData() => CreateScheduleData();
        internal void RunPruneStatesOutsideSchedule() => PruneStatesOutsideSchedule(removeOnlyTerminalStates: false);

        #endregion

        private void CreateScheduleData()
        {
            var now = _clock.UtcNow;
            foreach (var item in _schedule)
            {
                if (_states.ContainsKey(item.Id))
                {
                    continue;
                }

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

        private async UniTask ReloadScheduleAsync(bool removeAllStaleStates, CancellationToken ct)
        {
            var loadedSchedule = await LoadAndValidateScheduleAsync(ct);
            _schedule = BuildMergedRuntimeSchedule(_schedule, loadedSchedule);
            PruneStatesOutsideSchedule(removeOnlyTerminalStates: !removeAllStaleStates);
            CreateScheduleData();
            await _stateStore.SaveAsync(_states, ct);
        }

        private async UniTask<List<ScheduleItem>> LoadAndValidateScheduleAsync(CancellationToken ct)
        {
            var loadedSchedule = await _scheduleProvider.LoadAsync(ct);

            var validationErrors = await _scheduleValidator.ValidateAsync(loadedSchedule, ct);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException("Schedule validation failed: " + string.Join("; ", validationErrors));
            }

            return loadedSchedule
                .OrderBy(x => x.StreamId)
                .ThenByDescending(x => x.Priority)
                .ThenBy(x => x.StartTimeUtc)
                .ToList();
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

        private List<ScheduleItem> BuildMergedRuntimeSchedule(
            IReadOnlyCollection<ScheduleItem> currentRuntimeSchedule,
            IReadOnlyCollection<ScheduleItem> loadedSchedule)
        {
            var mergedRuntimeSchedule = BuildRuntimeSchedule(loadedSchedule);
            if (currentRuntimeSchedule == null || currentRuntimeSchedule.Count == 0)
            {
                return mergedRuntimeSchedule;
            }

            var knownIds = new HashSet<string>(mergedRuntimeSchedule.Select(item => item.Id), StringComparer.Ordinal);
            foreach (var item in currentRuntimeSchedule)
            {
                if (item == null || knownIds.Contains(item.Id))
                {
                    continue;
                }

                if (!RequiresRuntimeRetention(item.Id))
                {
                    continue;
                }

                mergedRuntimeSchedule.Add(item);
                knownIds.Add(item.Id);
            }

            return mergedRuntimeSchedule
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

            return RequiresRuntimeRetention(item.Id);
        }

        private bool RequiresRuntimeRetention(string scheduleItemId)
        {
            if (!_states.TryGetValue(scheduleItemId, out var state) || state == null)
            {
                return false;
            }

            if (IsInProgressState(state.State))
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

            while (safetyCounter-- > 0)
            {
                ct.ThrowIfCancellationRequested();

                var active = FindActive(items);
                if (active != null)
                {
                    await _engine.ProcessActiveEventAsync(active, now, ct);

                    if (!_states.TryGetValue(active.Id, out var activeStateData)
                        || activeStateData.State is EventInstanceState.Completed
                            or EventInstanceState.Failed
                            or EventInstanceState.Cancelled)
                    {
                        continue;
                    }

                    return;
                }

                var candidate = FindStartCandidate(items, now);
                if (candidate == null)
                {
                    return;
                }

                await _engine.StartEventAsync(candidate, ct);

                if (!_states.TryGetValue(candidate.Id, out var state) || state.State != EventInstanceState.Completed)
                {
                    return;
                }
            }
        }

        private ScheduleItem FindActive(IList<ScheduleItem> streamItems)
        {
            for (var i = 0; i < streamItems.Count; i++)
            {
                if (!_states.TryGetValue(streamItems[i].Id, out var stateData))
                {
                    continue;
                }

                if (IsInProgressState(stateData.State))
                {
                    return streamItems[i];
                }
            }

            return null;
        }

        private static readonly TimeSpan StartSkew = TimeSpan.FromSeconds(0);

        private ScheduleItem FindStartCandidate(IList<ScheduleItem> streamItems, DateTimeOffset now)
        {
            for (var i = 0; i < streamItems.Count; i++)
            {
                var item = streamItems[i];
                if (!_states.TryGetValue(item.Id, out var stateData))
                {
                    continue;
                }

                if (stateData.State != EventInstanceState.Pending)
                {
                    continue;
                }

                var startGate = item.StartTimeUtc - StartSkew;
                if (startGate > now || now >= item.EndTimeUtc)
                {
                    continue;
                }

                return item;
            }

            return null;
        }

        private void PruneStatesOutsideSchedule(bool removeOnlyTerminalStates)
        {
            if (_states.Count == 0)
            {
                return;
            }

            var runtimeScheduleIds = new HashSet<string>(_schedule.Select(item => item.Id), StringComparer.Ordinal);

            var staleIds = _states.Keys.Where(id => !runtimeScheduleIds.Contains(id)).ToList();
            for (var i = 0; i < staleIds.Count; i++)
            {
                if (removeOnlyTerminalStates &&
                    _states.TryGetValue(staleIds[i], out var state) &&
                    !IsTerminalState(state.State))
                {
                    continue;
                }

                _states.Remove(staleIds[i]);
                _engine.CleanupItem(staleIds[i]);
            }
        }

        private static bool IsInProgressState(EventInstanceState state)
        {
            return state is EventInstanceState.Active
                or EventInstanceState.Starting
                or EventInstanceState.Ending
                or EventInstanceState.Settling;
        }

        private static bool IsTerminalState(EventInstanceState state)
        {
            return state is EventInstanceState.Completed
                or EventInstanceState.Failed
                or EventInstanceState.Cancelled;
        }
    }
}
