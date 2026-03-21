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

        private IInventoryItemUseHandler _inventoryItemUseHandler;
        
        public CardCollectionInventoryIntegration(ICardCollectionWindowOpener cardCollectionWindowOpener)
        {
            _cardCollectionWindowOpener = cardCollectionWindowOpener ?? throw new ArgumentNullException(nameof(cardCollectionWindowOpener));
        }

        public void AttachAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_attached) return;

            var inventoryRoot = InventoryCompositionRegistry.Resolve();
            if (inventoryRoot == null)
            {
                Debug.LogWarning("Failed to Resolve IInventoryCompositionRoot.");
                return;
            }

            inventoryRoot.GetCategoryRegistry().Register(new CardsItemCategory());
            _inventoryItemUseHandler = new CardPackInventoryUseHandler(_cardCollectionWindowOpener);
            inventoryRoot.AddUseHandler(_inventoryItemUseHandler);

            _attached = true;
        }

        public void DetachAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var inventoryRoot = InventoryCompositionRegistry.Resolve();
            if (inventoryRoot == null)
            {
                Debug.LogWarning("Failed to Resolve IInventoryCompositionRoot.");
                return;
            }
            inventoryRoot.RemoveUseHandler(_inventoryItemUseHandler);
            _attached = false;
        }
    }
}