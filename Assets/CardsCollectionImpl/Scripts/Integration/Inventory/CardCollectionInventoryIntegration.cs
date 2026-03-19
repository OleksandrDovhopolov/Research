using System;
using System.Threading;
using CardCollection.Core;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionInventoryIntegration
    {
        private readonly IWindowPresenter _windowPresenter;
        private readonly ICardCollectionModule _module;
        private readonly ICardCollectionReader _reader;
        private bool _attached;

        private IInventoryItemUseHandler _inventoryItemUseHandler;
        
        public CardCollectionInventoryIntegration(
            IWindowPresenter windowPresenter,
            ICardCollectionModule module,
            ICardCollectionReader reader)
        {
            _windowPresenter = windowPresenter ?? throw new ArgumentNullException(nameof(windowPresenter));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
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
            _inventoryItemUseHandler = new CardPackInventoryUseHandler(_windowPresenter, _module, _reader);
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