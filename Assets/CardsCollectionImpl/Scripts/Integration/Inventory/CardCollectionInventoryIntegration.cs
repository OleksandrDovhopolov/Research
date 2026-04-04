using System;
using Inventory.API;

namespace CardCollectionImpl
{
    public sealed class CardCollectionInventoryIntegration
    {
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _handlerStorage;
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly ICardCollectionWindowCoordinator _cardCollectionWindowCoordinator;
        
        private bool _attached;
        private ItemCategory _itemCategory;
        private IInventoryItemUseHandler _inventoryItemUseHandler;
        
        public CardCollectionInventoryIntegration(
            IItemCategoryRegistry itemCategoryRegistry, 
            IInventoryUseHandlerStorage handlerStorage,
            ICardCollectionModule collectionModule,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionWindowCoordinator cardCollectionWindowCoordinator)
        {
            _handlerStorage = handlerStorage ?? throw new ArgumentNullException(nameof(handlerStorage));
            _itemCategoryRegistry = itemCategoryRegistry ?? throw new ArgumentNullException(nameof(itemCategoryRegistry));
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _cardCollectionWindowCoordinator = cardCollectionWindowCoordinator ?? throw new ArgumentNullException(nameof(cardCollectionWindowCoordinator));
        }

        public void Attach()
        {
            if (_attached) return;
            
            _itemCategory = new CardsItemCategory(CardsConfig.CardPack);
            _itemCategoryRegistry.Register(_itemCategory);
            
            _inventoryItemUseHandler = new CardPackInventoryUseHandler(_collectionModule, _pointsAccount, _cardCollectionWindowCoordinator);
            _handlerStorage.AddUseHandler(_inventoryItemUseHandler);

            _attached = true;
        }

        public void Detach()
        {
            _itemCategoryRegistry.RemoveRegister(_itemCategory);
            _handlerStorage.RemoveUseHandler(_inventoryItemUseHandler);

            _inventoryItemUseHandler = null;
            _itemCategory = null;
            _attached = false;
        }
    }
}