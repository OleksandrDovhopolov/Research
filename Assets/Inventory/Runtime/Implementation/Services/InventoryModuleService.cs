using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.Core;
using R3;

namespace Inventory.Implementation.Services
{
    public sealed class InventoryModuleService : IInventoryService, IDisposable
    {
        private readonly AddItemSystem _addItemSystem;
        private readonly RemoveItemSystem _removeItemSystem;
        private readonly InventoryQuerySystem _querySystem;
        private readonly IInventoryStorage _storage;
        private readonly Subject<InventoryChangedEvent> _inventoryChangedSubject = new();

        public InventoryModuleService(IInventoryStorage storage = null)
        {
            var world = new InventoryWorld();
            _addItemSystem = new AddItemSystem(world);
            _removeItemSystem = new RemoveItemSystem(world);
            _querySystem = new InventoryQuerySystem(world);
            _storage = storage ?? new InMemoryInventoryStorage();
        }

        public Observable<InventoryChangedEvent> OnInventoryChanged => _inventoryChangedSubject;

        public async Task AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var changed = _addItemSystem.Execute(itemDelta);
            if (!changed)
            {
                return;
            }

            await PublishAndPersistAsync(itemDelta.OwnerId, cancellationToken);
        }

        public async Task RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var changed = _removeItemSystem.Execute(itemDelta);
            if (!changed)
            {
                return;
            }

            await PublishAndPersistAsync(itemDelta.OwnerId, cancellationToken);
        }

        public Task<IReadOnlyList<InventoryItemView>> GetItemsAsync(
            string ownerId,
            InventoryItemCategory category,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = _querySystem.Execute(ownerId, category);
            return Task.FromResult(items);
        }

        public void Dispose()
        {
            _inventoryChangedSubject?.OnCompleted();
            _inventoryChangedSubject?.Dispose();
        }

        private async Task PublishAndPersistAsync(string ownerId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var regularItems = _querySystem.Execute(ownerId, InventoryItemCategory.Regular);
            var cardPackItems = _querySystem.Execute(ownerId, InventoryItemCategory.CardPack);
            var mergedItems = new List<InventoryItemView>(regularItems.Count + cardPackItems.Count);
            mergedItems.AddRange(regularItems);
            mergedItems.AddRange(cardPackItems);

            await _storage.SaveAsync(ownerId, mergedItems, cancellationToken);

            _inventoryChangedSubject.OnNext(new InventoryChangedEvent(
                ownerId,
                regularItems,
                cardPackItems,
                DateTime.UtcNow));
        }
    }
}
