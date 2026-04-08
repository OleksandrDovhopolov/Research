using System;
using System.Threading;
using CardCollection.Core;
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
        
        private readonly IEventCardsStorage _eventCardsStorage;
        private readonly ICardCollectionSessionFacade _sessionFacade;
        private readonly ICardCollectionRuntimeBuilder _collectionRuntimeBuilder;
        private readonly ICardCollectionSettlementCleanupService _settlementCleanupService;
        
        public CardCollectionLiveOpsController(
            IEventModelFactory modelFactory,
            IEventCardsStorage eventCardsStorage,
            ICardCollectionSettlementCleanupService settlementCleanupService,
            ICardCollectionSessionFacade sessionFacade,
            ICardCollectionRuntimeBuilder collectionRuntimeBuilder) : base("CardCollection", modelFactory)
        {
            _sessionFacade = sessionFacade;
            _eventCardsStorage = eventCardsStorage;
            _settlementCleanupService = settlementCleanupService;
            _collectionRuntimeBuilder = collectionRuntimeBuilder;
        }
        
        protected override async UniTask OnStartAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            Debug.LogWarning($"[CardCollectionRuntime] OnStartAsync: {model.EventId}");
    
            await CloseSessionInternalAsync(ct : ct);

            CardCollectionSession newSession = null;
            try
            {
                var firstStart = !state.StartInvoked;
                
                newSession = await _collectionRuntimeBuilder.BuildAsync(model, ct);
                await newSession.StartAsync(model, CurrentSchedule, firstStart, ct);

                ct.ThrowIfCancellationRequested();
                
                _cardCollectionSession = newSession;
                _sessionFacade.SetActiveSession(_cardCollectionSession.Context);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError($"[CardCollectionRuntime] Failed to start session: {e}");
                if (newSession != null) await CloseSessionInternalAsync(newSession, ct);
                throw;
            }
            catch (OperationCanceledException)
            {
                if (newSession != null) await CloseSessionInternalAsync(newSession, ct);
                throw;
            }
        }
        
        protected override async UniTask OnUpdateAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            if (_cardCollectionSession == null)
                return;
            await _cardCollectionSession.UpdateAsync(ct);
        }

        protected override async UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] End: {model.EventId}");
            await CloseSessionInternalAsync(ct : ct);
        }

        protected override async UniTask OnSettlementAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] Settle: {model.EventId}");
            await _settlementCleanupService.CleanupUnusedPacksAsync(model, ct);
            await _eventCardsStorage.DeleteAsync(model.EventId, ct);
        }
        
        private async UniTask CloseSessionInternalAsync(CardCollectionSession session = null, CancellationToken ct = default)
        {
            var targetSession = session ?? _cardCollectionSession;
            if (targetSession == null) return;

            try
            {
                await targetSession.StopAsync(ct);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardCollectionRuntime] Error during session stop: {e}");
            }
            finally
            {
                targetSession.Dispose();
        
                if (ReferenceEquals(targetSession, _cardCollectionSession))
                {
                    _cardCollectionSession = null;
                    _sessionFacade.ClearSession();
                    Debug.LogWarning($"[CardCollectionRuntime] CloseSessionInternalAsync");
                }
            }
        }
    }
}
