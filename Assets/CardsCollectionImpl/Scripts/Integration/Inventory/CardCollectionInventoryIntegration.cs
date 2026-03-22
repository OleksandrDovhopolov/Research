using System;
using System.Threading;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionInventoryIntegration
    {
        private readonly ICardCollectionWindowOpener _cardCollectionWindowOpener;
        
        private bool _attached;
        private ItemCategory _itemCategory;
        private IInventoryItemUseHandler _inventoryItemUseHandler;
        
        public CardCollectionInventoryIntegration(ICardCollectionWindowOpener cardCollectionWindowOpener)
        {
            _cardCollectionWindowOpener = cardCollectionWindowOpener ?? throw new ArgumentNullException(nameof(cardCollectionWindowOpener));
        }

        public void Attach()
        {
            if (_attached) return;

            var inventoryRoot = InventoryCompositionRegistry.Resolve();
            if (inventoryRoot == null)
            {
                Debug.LogWarning("Failed to Resolve IInventoryCompositionRoot.");
                return;
            }

            _itemCategory = new CardsItemCategory(CardsConfig.CardPack);
            inventoryRoot.GetCategoryRegistry().Register(_itemCategory);
            
            _inventoryItemUseHandler = new CardPackInventoryUseHandler(_cardCollectionWindowOpener);
            inventoryRoot.AddUseHandler(_inventoryItemUseHandler);

            _attached = true;
        }

        public void Detach()
        {
            var inventoryRoot = InventoryCompositionRegistry.Resolve();
            if (inventoryRoot == null)
            {
                Debug.LogWarning("Failed to Resolve IInventoryCompositionRoot.");
                return;
            }
            
            inventoryRoot.GetCategoryRegistry().RemoveRegister(_itemCategory);
            inventoryRoot.RemoveUseHandler(_inventoryItemUseHandler);

            _inventoryItemUseHandler = null;
            _itemCategory = null;
            _attached = false;
        }
    }
}