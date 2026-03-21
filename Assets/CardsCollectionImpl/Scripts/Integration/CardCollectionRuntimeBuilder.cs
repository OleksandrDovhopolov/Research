using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Inventory.API;
using Resources.Core;
using UIShared;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRuntimeBuilder : ICardCollectionRuntimeBuilder
    {
        private const string InventoryOwnerId = "player_1";
        
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly ICardPackProvider _cardPackProvider;

        public CardCollectionRuntimeBuilder(
            UIManager uiManager,
            IHUDService hudService,
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            ICardPackProvider cardPackProvider)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _resourceManager = resourceManager;
            _inventoryService = inventoryService;
            _cardPackProvider = cardPackProvider;
        }
        
        public async UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (model == null)
            {
                throw new ArgumentNullException($"CardCollectionEventModel is null {nameof(model)}");
            }
            
            var compositionRoot = CardCollectionCompositionRegistry.Resolve();
            if (compositionRoot == null)
            {
                throw new InvalidOperationException("Composition root not found");
            }
            
            var moduleConfig = compositionRoot.CreateModuleConfig(_cardPackProvider, model.CollectionId);
            var module = new CardCollectionModule(moduleConfig);
            //await module.InitializeAsync(ct);
            
            var rewardDefinitionFactory = await GetOrCreateRewardDefinitionFactory(compositionRoot, ct);
            var rewardHandler = InitializeRewardHandlerAsync(rewardDefinitionFactory);

            var snapshotService = new CollectionProgressSnapshotService();
            var windowOpener = CreateCardPackWindowOpener(compositionRoot, module, snapshotService, rewardHandler, rewardDefinitionFactory);
            
            var hudPresenter = new CardCollectionHudPresenter(_hudService, windowOpener);
            var inventoryIntegration = new CardCollectionInventoryIntegration(windowOpener);

            return new CardCollectionSession(
                module,
                hudPresenter,
                rewardHandler,
                inventoryIntegration,
                snapshotService);
        }
        
        private ICardCollectionRewardHandler InitializeRewardHandlerAsync(IRewardDefinitionFactory rewardDefinitionFactory)
        {
            //TODO GameRewardGrantService  move to DI / constructor ? 
            var rewardGrantService = new GameRewardGrantService(_resourceManager, _inventoryService, InventoryOwnerId);

            var rewardHandler = new CardCollectionRewardHandler(rewardGrantService, rewardDefinitionFactory);
            
            //await rewardHandler.InitializeAsync(ct);
            
            return rewardHandler;
        }
        
        private ICardCollectionWindowOpener CreateCardPackWindowOpener(
            ICardCollectionCompositionRoot compositionRoot,
            CardCollectionModule module,
            ICollectionProgressSnapshotService snapshotService,
            ICardCollectionRewardHandler rewardHandler,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            var exchangeOfferProvider = compositionRoot.CreateExchangeOfferProvider(rewardHandler);
            
            var cardCollectionWindowOpener = new CardCollectionWindowOpener(
                _uiManager, 
                module, 
                module, 
                module,
                exchangeOfferProvider,
                rewardDefinitionFactory,
                snapshotService);
            
            return cardCollectionWindowOpener;
        }

        
        private async UniTask<IRewardDefinitionFactory> GetOrCreateRewardDefinitionFactory(ICardCollectionCompositionRoot compositionRoot, CancellationToken ct = default)
        {
            var configs = await _cardPackProvider.GetCardConfigsAsync(ct);

            var rewardDefinitionFactory = compositionRoot.CreateRewardDefinitionFactory(configs);
            return rewardDefinitionFactory;
        }
    }
}