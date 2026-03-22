using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRuntimeBuilder : ICardCollectionRuntimeBuilder
    {
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ICardPackProvider _cardPackProvider;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _inventoryUseHandlerStorage;

        public CardCollectionRuntimeBuilder(
            UIManager uiManager,
            IHUDService hudService,
            ICardPackProvider cardPackProvider,
            ExchangePacksConfig exchangePacksConfig,
            IRewardGrantService rewardGrantService,
            IItemCategoryRegistry  itemCategoryRegistry,
            IInventoryUseHandlerStorage inventoryUseHandlerStorage)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _cardPackProvider = cardPackProvider;
            _exchangePacksConfig = exchangePacksConfig;
            _rewardGrantService = rewardGrantService;
            _itemCategoryRegistry = itemCategoryRegistry;
            _inventoryUseHandlerStorage = inventoryUseHandlerStorage;
        }
        
        public async UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (model == null)
            {
                throw new ArgumentNullException($"CardCollectionEventModel is null {nameof(model)}");
            }
            
            var moduleConfig = CreateModuleConfig(model.CollectionId);
            var module = new CardCollectionModule(moduleConfig);
            
            var rewardDefinitionFactory = await GetRewardDefinitionFactory(ct);
            var rewardHandler = GetRewardHandler(rewardDefinitionFactory);

            var snapshotService = new CollectionProgressSnapshotService();
            var windowOpener = CreateCardPackWindowOpener(module, snapshotService, rewardHandler, rewardDefinitionFactory);
            
            var hudPresenter = new CardCollectionHudPresenter(_hudService, windowOpener);
            var inventoryIntegration = new CardCollectionInventoryIntegration(_itemCategoryRegistry, _inventoryUseHandlerStorage, windowOpener);
            
            var context = new CardCollectionSessionContext(module, module, module, windowOpener);
            
            return new CardCollectionSession(
                context,
                module,
                hudPresenter,
                rewardHandler,
                inventoryIntegration,
                snapshotService);
        }
        
        private ICardCollectionRewardHandler GetRewardHandler(IRewardDefinitionFactory rewardDefinitionFactory)
        {
            var rewardHandler = new CardCollectionRewardHandler(_rewardGrantService, rewardDefinitionFactory);
            
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
        
        private async UniTask<IRewardDefinitionFactory> GetRewardDefinitionFactory(CancellationToken ct = default)
        {
            var configs = await _cardPackProvider.GetCardConfigsAsync(ct);
            var rewardDefinitionFactory = new RewardDefinitionFactory(_exchangePacksConfig, configs);
            return rewardDefinitionFactory;
        }
        
        private CardCollectionModuleConfig CreateModuleConfig(string eventId)
        {
            return new CardCollectionModuleConfig(
                _cardPackProvider,
                new JsonEventCardsStorage(),
                new DefaultCardDefinitionProvider(),
                new ProbabilityBasedCardSelector(PackRulesConfig.CreateDefaultRules()),
                CardsCollectionPointsCalculator.Instance,
                eventId);
        }
    }
}