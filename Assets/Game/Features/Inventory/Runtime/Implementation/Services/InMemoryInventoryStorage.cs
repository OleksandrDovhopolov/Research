using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;

namespace Inventory.Implementation.Services
{
    public sealed class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly Dictionary<string, List<InventoryItemView>> _storage = new();
        private readonly SaveService _saveService;

        public InMemoryInventoryStorage(SaveService saveService)
        {
            _saveService = saveService;
        }

        public async UniTask<IReadOnlyList<InventoryItemView>> LoadAsync(
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOwnerId(ownerId);
            await _saveService.LoadAllAsync(cancellationToken);

            if (_storage.TryGetValue(ownerId, out var savedItems))
            {
                return savedItems.ToArray();
            }

            var loadedItemsData = await _saveService.GetReadonlyModuleAsync(data =>
            {
                var resolvedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? "player_1" : ownerId;
                var owner = data.Inventory.Owners.Find(x => x.OwnerId == resolvedOwnerId);
                return owner?.Items?.ConvertAll(x => new InventoryItemSaveData
                {
                    OwnerId = x.OwnerId,
                    ItemId = x.ItemId,
                    StackCount = x.StackCount,
                    CategoryId = x.CategoryId,
                }) ?? new List<InventoryItemSaveData>();
            }, cancellationToken);
            var loadedItems = new List<InventoryItemView>(loadedItemsData.Count);
            foreach (var item in loadedItemsData)
            {
                loadedItems.Add(new InventoryItemView(
                    item.OwnerId,
                    item.ItemId,
                    item.StackCount,
                    item.CategoryId));
            }

            _storage[ownerId] = loadedItems;
            return loadedItems.ToArray();
        }

        public async UniTask SaveAsync(
            string ownerId,
            IReadOnlyList<InventoryItemView> items,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOwnerId(ownerId);
            await _saveService.LoadAllAsync(cancellationToken);

            _storage[ownerId] = new List<InventoryItemView>(items);
            var saveItems = new List<InventoryItemSaveData>(_storage[ownerId].Count);
            foreach (var item in _storage[ownerId])
            {
                saveItems.Add(new InventoryItemSaveData
                {
                    OwnerId = ownerId,
                    ItemId = item.ItemId,
                    StackCount = item.StackCount,
                    CategoryId = item.CategoryId,
                });
            }

            await _saveService.UpdateModuleAsync(data => data.Inventory, inventory =>
            {
                var resolvedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? "player_1" : ownerId;
                var owner = inventory.Owners.Find(x => x.OwnerId == resolvedOwnerId);
                if (owner == null)
                {
                    owner = new InventoryOwnerSaveData { OwnerId = resolvedOwnerId };
                    inventory.Owners.Add(owner);
                }

                owner.Items = saveItems;
            }, cancellationToken);
        }

        private static void ValidateOwnerId(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                throw new System.ArgumentException("Owner ID cannot be null or empty", nameof(ownerId));
            }
        }
    }
}
