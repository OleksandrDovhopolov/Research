using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRuntimeBuilder : ICardCollectionRuntimeBuilder
    {
        //TODO move this to CardCollectionEventModel Params
        private const string CardGroupsConfigFileName = "cardGroups";
        private const string CardsConfigFileName = "cardCollection";
        private const string PacksConfigFileName = "card_packs_config";
        
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _inventoryUseHandlerStorage;

        private readonly ICardPointsCalculator _cardPointsCalculator;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly ICardPackProvider _cardPackProvider;
        private readonly ICardsConfigProvider _cardsConfigProvider;
        private readonly ICardGroupsConfigProvider _cardGroupsConfigProvider;
        
        public CardCollectionRuntimeBuilder(
            UIManager uiManager,
            IHUDService hudService,
            ICardPackProvider cardPackProvider,
            ICardPointsCalculator pointsCalculator,
            ICardsConfigProvider cardsConfigProvider,
            ICardGroupsConfigProvider cardGroupsConfigProvider,
            ExchangePacksConfig exchangePacksConfig,
            IRewardGrantService rewardGrantService,
            IItemCategoryRegistry  itemCategoryRegistry,
            ICardCollectionCacheService  cardCollectionCacheService,
            IInventoryUseHandlerStorage inventoryUseHandlerStorage)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _cardPackProvider = cardPackProvider;
            _cardPointsCalculator = pointsCalculator;
            _cardsConfigProvider = cardsConfigProvider;
            _cardGroupsConfigProvider = cardGroupsConfigProvider;
            _exchangePacksConfig = exchangePacksConfig;
            _rewardGrantService = rewardGrantService;
            _itemCategoryRegistry = itemCategoryRegistry;
            _inventoryUseHandlerStorage = inventoryUseHandlerStorage;
            _cardCollectionCacheService = cardCollectionCacheService;
        }
        
        public async UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (model == null)
            {
                throw new ArgumentNullException($"CardCollectionEventModel is null {nameof(model)}");
            }
            
            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(PacksConfigFileName, ct),
                _cardsConfigProvider.LoadAsync(CardsConfigFileName, ct),
                _cardGroupsConfigProvider.LoadAsync(CardGroupsConfigFileName, ct)
            );
            
            var staticData = new CardCollectionStaticData 
            {
                Packs = _cardPackProvider.Data,
                Cards = _cardsConfigProvider.Data,
                Groups = _cardGroupsConfigProvider.Data
            };

            _cardCollectionCacheService.Initialize(staticData.Cards);
            
            var moduleConfig = CreateModuleConfig(staticData, model.CollectionId);
            var module = new CardCollectionModule(moduleConfig);
            
            var rewardDefinitionFactory = GetRewardDefinitionFactory(staticData.Packs);
            
            var rewardsConfig = await AddressablesWrapper.LoadFromTask<CardCollectionRewardsConfigSO>(model.RewardsConfigAddress);
            var rewardHandler = GetRewardHandler(rewardsConfig, rewardDefinitionFactory);

            var snapshotService = new CollectionProgressSnapshotService(_cardCollectionCacheService, staticData.Groups);
            var windowOpener = CreateCardPackWindowOpener(module, snapshotService, rewardHandler, rewardDefinitionFactory);
            
            var hudPresenter = new CardCollectionHudPresenter(_hudService, windowOpener);
            var inventoryIntegration = new CardCollectionInventoryIntegration(_itemCategoryRegistry, _inventoryUseHandlerStorage, windowOpener);
            
            var context = new CardCollectionSessionContext(module, module, module, windowOpener);
            
            return new CardCollectionSession(
                context,
                module,
                hudPresenter,
                rewardHandler,
                rewardsConfig,
                inventoryIntegration,
                snapshotService);
        }
        
        private ICardCollectionRewardHandler GetRewardHandler(CardCollectionRewardsConfigSO rewardsConfig, IRewardDefinitionFactory rewardDefinitionFactory)
        {
            var rewardHandler = new CardCollectionRewardHandler(rewardsConfig, _rewardGrantService, rewardDefinitionFactory);
            
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
                _cardsConfigProvider,
                exchangeOfferProvider,
                rewardDefinitionFactory,
                _cardCollectionCacheService,
                snapshotService);
            
            return cardCollectionWindowOpener;
        }
        
        private IRewardDefinitionFactory GetRewardDefinitionFactory(IReadOnlyList<CardPackConfig> packsConfig)
        {
            var rewardDefinitionFactory = new RewardDefinitionFactory(_exchangePacksConfig, packsConfig);
            return rewardDefinitionFactory;
        }
        
        private CardCollectionModuleConfig CreateModuleConfig(CardCollectionStaticData staticData, string eventId)
        {
            return new CardCollectionModuleConfig(
                _cardPackProvider,
                new JsonEventCardsStorage(),
                new DefaultCardDefinitionProvider(staticData.Cards),
                new ProbabilityBasedCardSelector(PackRulesConfig.CreateDefaultRules()),
                _cardPointsCalculator,
                eventId);
        }
    }
    
    public class CardCollectionStaticData
    {
        public IReadOnlyList<CardPackConfig> Packs { get; set; }
        public IReadOnlyList<CardConfig> Cards { get; set; }
        public IReadOnlyList<CardCollectionGroupConfig> Groups { get; set; }
    }
}