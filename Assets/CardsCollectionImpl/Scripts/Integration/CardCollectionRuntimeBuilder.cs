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
        private readonly ExchangePacksConfig _exchangePacksConfig;

        public CardCollectionRuntimeBuilder(
            UIManager uiManager,
            IHUDService hudService,
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            ICardPackProvider cardPackProvider,
            ExchangePacksConfig exchangePacksConfig)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _resourceManager = resourceManager;
            _inventoryService = inventoryService;
            _cardPackProvider = cardPackProvider;
            _exchangePacksConfig = exchangePacksConfig;
        }
        
        public async UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (model == null)
            {
                throw new ArgumentNullException($"CardCollectionEventModel is null {nameof(model)}");
            }
            
            var moduleConfig = CreateModuleConfig(_cardPackProvider, model.CollectionId);
            var module = new CardCollectionModule(moduleConfig);
            
            var rewardDefinitionFactory = await GetOrCreateRewardDefinitionFactory(ct);
            var rewardHandler = InitializeRewardHandlerAsync(rewardDefinitionFactory);

            var snapshotService = new CollectionProgressSnapshotService();
            var windowOpener = CreateCardPackWindowOpener(module, snapshotService, rewardHandler, rewardDefinitionFactory);
            
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
            
            return rewardHandler;
        }
        
        private ICardCollectionWindowOpener CreateCardPackWindowOpener(
            CardCollectionModule module,
            ICollectionProgressSnapshotService snapshotService,
            ICardCollectionRewardHandler rewardHandler,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            var exchangeOfferProvider = new ExchangeOfferProvider(_exchangePacksConfig, rewardHandler);
            
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
        
        private async UniTask<IRewardDefinitionFactory> GetOrCreateRewardDefinitionFactory(CancellationToken ct = default)
        {
            var configs = await _cardPackProvider.GetCardConfigsAsync(ct);
            var rewardDefinitionFactory = new RewardDefinitionFactory(_exchangePacksConfig, configs);
            return rewardDefinitionFactory;
        }
        
        private CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId)
        {
            IEventCardsStorage cardsStorage = new JsonEventCardsStorage();
            ICardDefinitionProvider cardDefinitionProvider = new DefaultCardDefinitionProvider();
            ICardSelector cardSelector = new ProbabilityBasedCardSelector(PackRulesConfig.CreateDefaultRules());

            return new CardCollectionModuleConfig(
                cardPackProvider,
                cardsStorage,
                cardDefinitionProvider,
                cardSelector,
                CardsCollectionPointsCalculator.Instance,
                eventId);
        }
    }
}