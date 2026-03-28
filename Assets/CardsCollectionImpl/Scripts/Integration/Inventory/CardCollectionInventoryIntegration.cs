using System;
using Inventory.API;

namespace CardCollectionImpl
{
    public sealed class CardCollectionInventoryIntegration
    {
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _handlerStorage;
        private readonly ICardCollectionWindowOpener _cardCollectionWindowOpener;
        
        private bool _attached;
        private ItemCategory _itemCategory;
        private IInventoryItemUseHandler _inventoryItemUseHandler;
        
        public CardCollectionInventoryIntegration(
            IItemCategoryRegistry itemCategoryRegistry, 
            IInventoryUseHandlerStorage handlerStorage,
            ICardCollectionWindowOpener cardCollectionWindowOpener)
        {
            _handlerStorage = handlerStorage ?? throw new ArgumentNullException(nameof(handlerStorage));
            _itemCategoryRegistry = itemCategoryRegistry ?? throw new ArgumentNullException(nameof(itemCategoryRegistry));
            _cardCollectionWindowOpener = cardCollectionWindowOpener ?? throw new ArgumentNullException(nameof(cardCollectionWindowOpener));
        }

        public void Attach()
        {
            if (_attached) return;
            
            _itemCategory = new CardsItemCategory(CardsConfig.CardPack);
            _itemCategoryRegistry.Register(_itemCategory);
            
            _inventoryItemUseHandler = new CardPackInventoryUseHandler(_cardCollectionWindowOpener);
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