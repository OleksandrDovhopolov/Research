using System;
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
            Debug.LogWarning($"[CardCollectionRuntime] OnStartAsync: {model.EventId}");
    
            await CloseSessionInternalAsync(ct : ct);

            CardCollectionSession newSession = null;
            try
            {
                newSession = await _collectionRuntimeBuilder.BuildAsync(model, ct);
                await newSession.StartAsync(CurrentSchedule, ct);

                ct.ThrowIfCancellationRequested();
                
                _cardCollectionSession = newSession;
                _featureFacade.SetActiveSession(_cardCollectionSession.Context);
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
            Debug.LogWarning($"[CardCollectionRuntime] OnUpdateAsync: {model.EventId}");
            await _cardCollectionSession.UpdateAsync(ct);
        }

        protected override async UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] End: {model.EventId}");
            await CloseSessionInternalAsync(ct : ct);
        }

        protected override UniTask OnSettlementAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] Settle: {model.EventId}");
            //TODO delete save file and make new with general data
            return _cardCollectionSession?.SettleAsync(ct) ?? UniTask.CompletedTask;
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
                    _featureFacade.ClearSession();
                }
            }
        }
    }
}
