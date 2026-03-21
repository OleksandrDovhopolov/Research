using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using EventOrchestration.Models;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionLiveOpsController : BaseLiveOpsController<CardCollectionEventModel>
    {
        private CardCollectionSession _cardCollectionSession;
        private CancellationTokenSource _rewardHandlersCts;
        
        private readonly ICardCollectionFeatureFacade _featureFacade;
        private readonly ICardCollectionRuntimeBuilder _collectionRuntimeBuilder;
        
        public CardCollectionLiveOpsController(
            IEventModelFactory modelFactory,
            ICardCollectionFeatureFacade featureFacade,
            ICardCollectionRuntimeBuilder collectionRuntimeBuilder) : base("CardCollection", modelFactory)
        {
            _featureFacade = featureFacade;
            _collectionRuntimeBuilder = collectionRuntimeBuilder;
        }
        
        protected override async UniTask OnStartAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            Debug.LogWarning($"[CardCollectionRuntime] OnStartAsync: {model.EventId}");
            
            _cardCollectionSession = await _collectionRuntimeBuilder.BuildAsync(model, ct);
            await _cardCollectionSession.StartAsync(CurrentSchedule, ct);
            
            _featureFacade.SetActiveSession(_cardCollectionSession.Context);
            
            await UniTask.CompletedTask;
        }
        
        protected override async UniTask OnUpdateAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] OnUpdateAsync: {model.EventId}");
            await _cardCollectionSession.UpdateAsync(ct);
        }

        protected override async UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            Debug.LogWarning($"[CardCollectionRuntime] End: {model.EventId}");
            
            await _cardCollectionSession.StopAsync(ct);
            
            _featureFacade.ClearSession();
        }

        protected override UniTask OnSettlementAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] Settle: {model.EventId}");
            return _cardCollectionSession.SettleAsync(ct);
        }
    }
}
