using CardCollection.Core;
using EventOrchestration.Models;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionFactory : ICardCollectionSessionFactory
    {
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly IRewardSpecProvider _rewardSpecProvider;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _inventoryUseHandlerStorage;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;

        public CardCollectionSessionFactory(
            UIManager uiManager,
            IHUDService hudService,
            IRewardSpecProvider rewardSpecProvider,
            IRewardGrantService rewardGrantService,
            IItemCategoryRegistry itemCategoryRegistry,
            IInventoryUseHandlerStorage inventoryUseHandlerStorage,
            ICardCollectionCacheService cardCollectionCacheService)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _rewardSpecProvider = rewardSpecProvider;
            _rewardGrantService = rewardGrantService;
            _itemCategoryRegistry = itemCategoryRegistry;
            _inventoryUseHandlerStorage = inventoryUseHandlerStorage;
            _cardCollectionCacheService = cardCollectionCacheService;
        }

        public CardCollectionSession Create(CardCollectionEventModel model, CardCollectionStaticData staticData, ICardCollectionApplicationFacade facade)
        {
            var rewardHandler = new CardCollectionRewardHandler(staticData, _rewardSpecProvider, _rewardGrantService);
            
            var snapshotBuilder = new CollectionProgressSnapshotBuilder(_cardCollectionCacheService, staticData.Groups);
            
            var exchangeOfferProvider = new ExchangeOfferProvider(staticData.Offers, rewardHandler);

            var windowCoordinator = new CardCollectionWindowCoordinator(_uiManager);

            var hudPresenter = new CardCollectionHudPresenter(_uiManager, _hudService);
            
            var newCardWindowFlow = new OpenPackFlow(facade, _cardCollectionCacheService, windowCoordinator);
            
            var cardPackInventoryUseHandler = new CardPackInventoryUseHandler(newCardWindowFlow);
            
            var inventoryIntegration = new CardCollectionInventoryIntegration(_itemCategoryRegistry, _inventoryUseHandlerStorage, cardPackInventoryUseHandler);
            
            var context = new CardCollectionSessionContext(newCardWindowFlow, windowCoordinator, facade);

            return new CardCollectionSession(
                _uiManager,
                context,
                hudPresenter,
                rewardHandler,
                inventoryIntegration,
                staticData,
                snapshotBuilder,
                exchangeOfferProvider);
        }
    }
}
