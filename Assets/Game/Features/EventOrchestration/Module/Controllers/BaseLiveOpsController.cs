using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration
{
    public abstract class BaseLiveOpsController<T> : IEventController where T : BaseGameEventModel
    {
        protected BaseLiveOpsController(string eventType)
        {
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        }

        public string EventType { get; }

        protected T CurrentModel { get; private set; }
        protected EventStateData CurrentState { get; private set; }
        protected ScheduleItem CurrentSchedule { get; private set; }

        public async UniTask InitializeAsync(ScheduleItem config, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (state == null) throw new ArgumentNullException(nameof(state));

            CurrentSchedule = config;
            CurrentState = state;
            var typedModel = await CreateModelAsync(config, ct);

            CurrentModel = typedModel ?? throw new InvalidOperationException($"Model factory returned null for event type '{EventType}'.");

            await OnInitializeModelAsync(CurrentModel, config, state, ct);
        }

        public UniTask OnStart(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureInitialized();
            return OnStartAsync(CurrentModel, CurrentState, ct);
        }

        public UniTask OnUpdate(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureInitialized();
            return OnUpdateAsync(CurrentModel, CurrentState, ct);
        }

        public UniTask OnEnd(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureInitialized();
            return OnEndAsync(CurrentModel, CurrentState, ct);
        }

        public UniTask ExecuteSettlement(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureInitialized();
            return OnSettlementAsync(CurrentModel, CurrentState, ct);
        }

        protected virtual UniTask OnInitializeModelAsync(T model, ScheduleItem config, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        protected abstract UniTask<T> CreateModelAsync(ScheduleItem config, CancellationToken ct);
        protected abstract UniTask OnStartAsync(T model, EventStateData state, CancellationToken ct);
        protected abstract UniTask OnUpdateAsync(T model, EventStateData state, CancellationToken ct);
        protected abstract UniTask OnEndAsync(T model, EventStateData state, CancellationToken ct);
        protected abstract UniTask OnSettlementAsync(T model, EventStateData state, CancellationToken ct);

        private void EnsureInitialized()
        {
            if (CurrentModel == null || CurrentState == null || CurrentSchedule == null)
            {
                throw new InvalidOperationException(
                    $"Controller '{GetType().Name}' is not initialized. Call {nameof(InitializeAsync)} first.");
            }
        }
    }
}
