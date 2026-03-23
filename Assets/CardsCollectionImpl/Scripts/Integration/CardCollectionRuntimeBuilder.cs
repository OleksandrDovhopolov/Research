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
        
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _inventoryUseHandlerStorage;

        private readonly ICardPackProvider _cardPackProvider;
        private readonly ICardsConfigProvider _cardsConfigProvider;
        private readonly ICardGroupsConfigProvider _cardGroupsConfigProvider;
        
        public CardCollectionRuntimeBuilder(
            UIManager uiManager,
            IHUDService hudService,
            ICardPackProvider cardPackProvider,
            ICardsConfigProvider cardsConfigProvider,
            ICardGroupsConfigProvider cardGroupsConfigProvider,
            ExchangePacksConfig exchangePacksConfig,
            IRewardGrantService rewardGrantService,
            IItemCategoryRegistry  itemCategoryRegistry,
            IInventoryUseHandlerStorage inventoryUseHandlerStorage)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _cardPackProvider = cardPackProvider;
            _cardsConfigProvider = cardsConfigProvider;
            _cardGroupsConfigProvider = cardGroupsConfigProvider;
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

            //TODO make list -> IReadOnlyList
            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(string.Empty, ct),
                _cardsConfigProvider.LoadAsync(CardsConfigFileName, ct),
                _cardGroupsConfigProvider.LoadAsync(CardGroupsConfigFileName, ct)
            );
            
            var staticData = new CardCollectionStaticData 
            {
                Packs = _cardPackProvider.Data,
                Cards = _cardsConfigProvider.Data,
                Groups = _cardGroupsConfigProvider.Data
            };
            
            var rewardDefinitionFactory = GetRewardDefinitionFactory(staticData.Packs);
            
            var rewardsConfig = await AddressablesWrapper.LoadFromTask<CardCollectionRewardsConfigSO>(model.RewardsConfigAddress);
            var rewardHandler = GetRewardHandler(rewardsConfig, rewardDefinitionFactory);

            var snapshotService = new CollectionProgressSnapshotService(staticData.Groups);
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
                exchangeOfferProvider,
                rewardDefinitionFactory,
                _cardGroupsConfigProvider,
                snapshotService);
            
            return cardCollectionWindowOpener;
        }
        
        private IRewardDefinitionFactory GetRewardDefinitionFactory(IReadOnlyList<CardPackConfig> packsConfig)
        {
            var rewardDefinitionFactory = new RewardDefinitionFactory(_exchangePacksConfig, packsConfig);
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
    
    public class CardCollectionStaticData
    {
        public IReadOnlyList<CardPackConfig> Packs { get; set; }
        public IReadOnlyList<CardConfig> Cards { get; set; }
        public IReadOnlyList<CardCollectionGroupConfig> Groups { get; set; }
        //init - error - the predefined type 'System.Runtime.CompilerServices.IsExternalInit' must be defined or imported in order to declare init-only setter
    }
}