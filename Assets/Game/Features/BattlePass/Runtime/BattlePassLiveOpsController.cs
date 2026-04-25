using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using EventOrchestration.Models;

namespace BattlePass
{
    public enum BattlePassLifecycleStatus
    {
        Inactive = 0,
        Upcoming = 1,
        Active = 2
    }

    public interface IBattlePassLifecycleState
    {
        BattlePassLifecycleStatus CurrentStatus { get; }
        bool IsActive { get; }
    }

    public sealed class BattlePassLifecycleState : IBattlePassLifecycleState
    {
        public BattlePassLifecycleStatus CurrentStatus { get; private set; } = BattlePassLifecycleStatus.Inactive;
        public bool IsActive => CurrentStatus == BattlePassLifecycleStatus.Active;

        public void SetStatus(BattlePassLifecycleStatus status)
        {
            CurrentStatus = status;
        }
    }

    public sealed class BattlePassEventModel : BaseGameEventModel
    {
    }

    public sealed class BattlePassEventModelFactory
    {
        public UniTask<BattlePassEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            var model = new BattlePassEventModel
            {
                EventId = item.Id,
                EventType = item.EventType,
                StreamId = item.StreamId,
                CollectionName = item.CustomParams != null && item.CustomParams.TryGetValue("title", out var title)
                    ? title
                    : string.Empty,
            };

            return UniTask.FromResult(model);
        }
    }

    public sealed class BattlePassLiveOpsController : BaseLiveOpsController<BattlePassEventModel>
    {
        private readonly BattlePassEventModelFactory _modelFactory;
        private readonly BattlePassLifecycleState _lifecycleState;

        public BattlePassLiveOpsController(
            BattlePassEventModelFactory modelFactory,
            BattlePassLifecycleState lifecycleState) : base("BattlePass")
        {
            _modelFactory = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));
            _lifecycleState = lifecycleState ?? throw new ArgumentNullException(nameof(lifecycleState));
        }

        protected override UniTask<BattlePassEventModel> CreateModelAsync(ScheduleItem config, CancellationToken ct)
        {
            return _modelFactory.CreateAsync(config, ct);
        }

        protected override UniTask OnStartAsync(BattlePassEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUpdateAsync(BattlePassEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnEndAsync(BattlePassEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _lifecycleState.SetStatus(BattlePassLifecycleStatus.Inactive);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnSettlementAsync(BattlePassEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }
    }
}
