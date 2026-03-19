using System;
using System.Threading;
using CardCollection.Core;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using Inventory.API;
using Resources.Core;
using UnityEngine;

namespace EventOrchestration.Controllers
{
    public sealed class CardCollectionController : BaseLiveOpsController<CardCollectionEventModel>
    {
        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly ICardPackProvider _cardPackProvider;

        private CancellationTokenSource _rewardHandlersCts;
        
        private CardCollectionModule _cardCollectionModule;
        private ICardCollectionRewardHandler _rewardHandler; 
        private IExchangeOfferProvider _exchangeOfferProvider;
        private IRewardDefinitionFactory _rewardDefinitionFactory;
        private CardCollectionInventoryIntegration _cardCollectionInventoryIntegration;

        public CardCollectionController(
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            IEventModelFactory modelFactory) : base("CardCollection", modelFactory)
        {
            _resourceManager = resourceManager;
            _inventoryService = inventoryService;
            _cardPackProvider = new JsonCardPackProvider();
        }
        
        //TODO remove this from field
        private const string InventoryOwnerId = "player_1";

        protected override async UniTask OnStartAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var compositionRoot = CardCollectionCompositionRegistry.Resolve();
            if (compositionRoot == null)
            {
                throw new InvalidOperationException("CardCollection composition root is not registered.");
            }
            
            var cardPackConfigs = await _cardPackProvider.GetCardConfigsAsync(ct);
            _rewardDefinitionFactory = compositionRoot.CreateRewardDefinitionFactory(cardPackConfigs);
            
            var debugRewardCreator = new DebugRewardCreator(
                _resourceManager, 
                _inventoryService,
                compositionRoot,
                _rewardDefinitionFactory,
                InventoryOwnerId);
            
            await debugRewardCreator.InitializeRewardHandlerAsync(ct);
            
            _rewardHandler = debugRewardCreator.RewardHandler;
            
            _exchangeOfferProvider = compositionRoot.CreateExchangeOfferProvider(_rewardHandler);
            
            if (_cardCollectionModule != null)
            {
                CancelAndDisposeRewardHandlersCts();
                _cardCollectionModule.Dispose();
                _cardCollectionModule = null;
            }

            var config = compositionRoot.CreateModuleConfig(_cardPackProvider, model.CollectionId);
            _cardCollectionModule = new CardCollectionModule(config);
            await _cardCollectionModule.InitializeAsync(ct);
            ResetRewardHandlersCts();
            
            _cardCollectionModule.OnGroupCompleted += GroupCompletedHandler;
            _cardCollectionModule.OnCollectionCompleted += CollectionCompletedHandler;
            
            var collectionData = await _cardCollectionModule.Load(ct);
            var windowPresenter = compositionRoot.CreateWindowPresenter(collectionData);
            
            _cardCollectionInventoryIntegration = new CardCollectionInventoryIntegration(windowPresenter, _cardCollectionModule, _cardCollectionModule);
            _cardCollectionInventoryIntegration.AttachAsync(ct);
            
            Debug.LogWarning($"[CardCollectionRuntime] Start: {model.EventId}, collection={model.CollectionId}");
            await UniTask.CompletedTask;
        }

        protected override UniTask OnUpdateAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] End: {model.EventId}");
            await UniTask.CompletedTask;

            if (_cardCollectionModule == null)
            {
                CancelAndDisposeRewardHandlersCts();
                return;
            }

            _cardCollectionInventoryIntegration?.DetachAsync(ct);

            _cardCollectionModule.OnGroupCompleted -= GroupCompletedHandler;
            _cardCollectionModule.OnCollectionCompleted -= CollectionCompletedHandler;
            _cardCollectionModule.Dispose();
            _cardCollectionModule = null;
            CancelAndDisposeRewardHandlersCts();
        }

        protected override UniTask OnSettlementAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] Settle: {model.EventId}");
            return UniTask.CompletedTask;
        }

        private void GroupCompletedHandler(CardGroupCompletedData groupCompletedData)
        {
            if (_rewardHandler == null)
            {
                Debug.LogWarning($"Failed to handler reward event. CardCollectionRewardHandler is null. Group id = {groupCompletedData.GroupId}");
                return;
            }

            if (_rewardHandlersCts == null || _rewardHandlersCts.IsCancellationRequested)
            {
                Debug.LogWarning($"Skip group reward handling for {groupCompletedData.GroupId}: token is cancelled.");
                return;
            }

            _rewardHandler.TryHandleGroupCompleted(groupCompletedData, _rewardHandlersCts.Token).Forget();
        }

        private void CollectionCompletedHandler(CardCollectionCompletedData collectionCompletedData)
        {
            if (_rewardHandler == null)
            {
                Debug.LogWarning("Failed to handle collection-completed reward event. CardCollectionRewardHandler is null.");
                return;
            }

            if (_rewardHandlersCts == null || _rewardHandlersCts.IsCancellationRequested)
            {
                Debug.LogWarning($"Skip collection reward handling for {collectionCompletedData.EventId}: token is cancelled.");
                return;
            }

            _rewardHandler.TryHandleCollectionCompleted(collectionCompletedData, _rewardHandlersCts.Token).Forget();
        }

        private void ResetRewardHandlersCts()
        {
            CancelAndDisposeRewardHandlersCts();
            _rewardHandlersCts = new CancellationTokenSource();
        }

        private void CancelAndDisposeRewardHandlersCts()
        {
            if (_rewardHandlersCts == null)
            {
                return;
            }

            _rewardHandlersCts.Cancel();
            _rewardHandlersCts.Dispose();
            _rewardHandlersCts = null;
        }

    }
}
