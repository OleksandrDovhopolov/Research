using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.Core;
using R3;
using UnityEngine;

namespace Inventory.Implementation.Services
{
    public sealed class InventoryModuleService : IInventoryService, IDisposable
    {
        private readonly AddItemSystem _addItemSystem;
        private readonly RemoveItemSystem _removeItemSystem;
        private readonly InventoryQuerySystem _querySystem;
        private readonly IInventoryStorage _storage;
        private readonly Subject<InventoryChangedEvent> _inventoryChangedSubject = new();
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private readonly HashSet<string> _loadedOwners = new();

        public InventoryModuleService(IInventoryStorage storage = null)
        {
            var world = new InventoryWorld();
            _addItemSystem = new AddItemSystem(world);
            _removeItemSystem = new RemoveItemSystem(world);
            _querySystem = new InventoryQuerySystem(world);
            _storage = storage ?? new InMemoryInventoryStorage();
        }

        public Observable<InventoryChangedEvent> OnInventoryChanged => _inventoryChangedSubject;

        public async UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureOwnerLoadedAsync(itemDelta.OwnerId, cancellationToken);
            var changed = _addItemSystem.Execute(itemDelta);
            Debug.LogWarning($"Test ownerId {itemDelta.OwnerId},  itemId {itemDelta.ItemId}, changed {changed}");
            if (!changed)
            {
                return;
            }

            await PublishAndPersistAsync(itemDelta.OwnerId, cancellationToken);
        }

        public async UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureOwnerLoadedAsync(itemDelta.OwnerId, cancellationToken);
            var changed = _removeItemSystem.Execute(itemDelta);
            if (!changed)
            {
                return;
            }

            await PublishAndPersistAsync(itemDelta.OwnerId, cancellationToken);
        }

        public async UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(string ownerId, string categoryId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureOwnerLoadedAsync(ownerId, cancellationToken);
            var items = _querySystem.Execute(ownerId, categoryId);
            return items;
        }

        public void Dispose()
        {
            _inventoryChangedSubject?.OnCompleted();
            _inventoryChangedSubject?.Dispose();
            _loadSemaphore.Dispose();
        }

        private async UniTask PublishAndPersistAsync(string ownerId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allItems = _querySystem.ExecuteAll(ownerId);
            var itemsByCategory = allItems
                .GroupBy(item => item.CategoryId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<InventoryItemView>)group.ToList());

            await _storage.SaveAsync(ownerId, allItems, cancellationToken);

            _inventoryChangedSubject.OnNext(new InventoryChangedEvent(
                ownerId,
                itemsByCategory,
                DateTime.UtcNow));
        }

        private async UniTask EnsureOwnerLoadedAsync(string ownerId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_loadedOwners.Contains(ownerId))
            {
                return;
            }

            await _loadSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_loadedOwners.Contains(ownerId))
                {
                    return;
                }

                var loadedItems = await _storage.LoadAsync(ownerId, cancellationToken);
                foreach (var item in loadedItems)
                {
                    var delta = new InventoryItemDelta(
                        item.OwnerId,
                        item.ItemId,
                        item.StackCount,
                        item.CategoryId);
                    _addItemSystem.Execute(delta);
                }

                _loadedOwners.Add(ownerId);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }
    }
}
