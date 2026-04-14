using System;
using Inventory.API;

namespace CardCollectionImpl
{
    public sealed class CardCollectionInventoryIntegration
    {
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _handlerStorage;
        private readonly IInventoryItemUseHandler _inventoryItemUseHandler;
        
        private bool _attached;
        private ItemCategory _itemCategory;
        
        public CardCollectionInventoryIntegration(
            IItemCategoryRegistry itemCategoryRegistry, 
            IInventoryUseHandlerStorage handlerStorage, 
            IInventoryItemUseHandler inventoryItemUseHandler)
        {
            _handlerStorage = handlerStorage ?? throw new ArgumentNullException(nameof(handlerStorage));
            _itemCategoryRegistry = itemCategoryRegistry ?? throw new ArgumentNullException(nameof(itemCategoryRegistry));
            _inventoryItemUseHandler = inventoryItemUseHandler ?? throw new ArgumentNullException(nameof(inventoryItemUseHandler));
        }

        public void Attach()
        {
            if (_attached) return;
            
            _itemCategory = new CardsItemCategory(CardCollectionGeneralConfig.CardPack);
            _itemCategoryRegistry.Register(_itemCategory);
            _handlerStorage.AddUseHandler(_inventoryItemUseHandler);

            _attached = true;
        }

        public void Detach()
        {
            _itemCategoryRegistry.RemoveRegister(_itemCategory);
            _handlerStorage.RemoveUseHandler(_inventoryItemUseHandler);

            _itemCategory = null;
            _attached = false;
        }
    }
}