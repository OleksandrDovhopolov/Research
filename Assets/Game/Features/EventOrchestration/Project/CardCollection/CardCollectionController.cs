using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Controllers
{
    public sealed class CardCollectionController : BaseLiveOpsController<CardCollectionEventModel>
    {
        private readonly ICardCollectionRuntime _runtime;

        public CardCollectionController(
            ICardCollectionRuntime runtime,
            IEventModelFactory modelFactory)
            : base("CardCollection", modelFactory)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        protected override UniTask OnStartAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _runtime.StartAsync(model, ct);
        }

        protected override UniTask OnUpdateAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _runtime.UpdateAsync(model, ct);
        }

        protected override UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _runtime.EndAsync(model, ct);
        }

        protected override UniTask OnSettlementAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _runtime.SettleAsync(model, ct);
        }
    }
}
