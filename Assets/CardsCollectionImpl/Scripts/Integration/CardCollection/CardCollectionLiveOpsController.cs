using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using EventOrchestration.Models;
using Inventory.API;
using Resources.Core;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionLiveOpsController : BaseLiveOpsController<CardCollectionEventModel>
    {
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly ICardPackProvider _cardPackProvider;
        private readonly ICardCollectionFeatureFacade _featureFacade;

        private CancellationTokenSource _rewardHandlersCts;

        private CardCollectionHudPresenter _cardCollectionHudPresenter;
        
        private CardCollectionModule _cardCollectionModule;
        private ICardCollectionRewardHandler _rewardHandler; 
        private IExchangeOfferProvider _exchangeOfferProvider;
        private IRewardDefinitionFactory _rewardDefinitionFactory;
        private CardCollectionInventoryIntegration _cardCollectionInventoryIntegration;

        public CardCollectionLiveOpsController(
            UIManager uiManager, 
            IHUDService hudService,
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            IEventModelFactory modelFactory,
            ICardCollectionFeatureFacade featureFacade) : base("CardCollection", modelFactory)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _resourceManager = resourceManager;
            _inventoryService = inventoryService;
            _featureFacade = featureFacade;
            //TODO remove new()
            _cardPackProvider = new JsonCardPackProvider();
        }
        
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
                _rewardDefinitionFactory);
            
            await debugRewardCreator.InitializeRewardHandlerAsync(ct);
            
            _rewardHandler = debugRewardCreator.RewardHandler;
            
            _exchangeOfferProvider = compositionRoot.CreateExchangeOfferProvider(_rewardHandler);
            
            if (_cardCollectionModule != null)
            {
                _cardCollectionModule.OnGroupCompleted -= GroupCompletedHandler;
                _cardCollectionModule.OnCollectionCompleted -= CollectionCompletedHandler;
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
            
            BindInventoryCategory();
            
            var collectionData = await _cardCollectionModule.Load(ct);
            BindHUDPresenter(collectionData);
            
            _featureFacade.SetActiveSession();
            
            await UniTask.CompletedTask;
        }

        private void BindInventoryCategory()
        {
            _cardCollectionInventoryIntegration = new CardCollectionInventoryIntegration(_uiManager, _cardCollectionModule, _cardCollectionModule);
            _cardCollectionInventoryIntegration.AttachAsync(_rewardHandlersCts.Token);
        }
        
        private void BindHUDPresenter(EventCardsSaveData collectionData)
        {
            var collectionProgressSnapshotService = new CollectionProgressSnapshotService();;
            collectionProgressSnapshotService.SetSnapshot(collectionData);
            
            _cardCollectionHudPresenter = new CardCollectionHudPresenter(
                _uiManager,
                _hudService,
                _cardCollectionModule,
                _cardCollectionModule,
                _cardCollectionModule,
                _exchangeOfferProvider,
                _rewardDefinitionFactory,
                collectionProgressSnapshotService);
            
            _cardCollectionHudPresenter.Bind(CurrentSchedule, _rewardHandlersCts.Token);
        }
        
        protected override UniTask OnUpdateAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnEndAsync(CardCollectionEventModel model, EventStateData state, CancellationToken ct)
        {
            //TODO close all card collection windows
            //TODO show card collection complete window 
            //TODO remove items from inventory and consume it
            
            await UniTask.CompletedTask;
            
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning($"[CardCollectionRuntime] End: {model.EventId}");
            
            _featureFacade.ClearSession();
            _cardCollectionHudPresenter?.Unbind();
            _cardCollectionInventoryIntegration?.DetachAsync(_rewardHandlersCts.Token);
            
            if (_cardCollectionModule == null)
            {
                CancelAndDisposeRewardHandlersCts();
                return;
            }
            
            
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
