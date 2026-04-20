using System;
using System.Collections.Generic;
using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal sealed class InventoryWorld
    {
        private int _nextEntityId = 1;

        private readonly HashSet<int> _entities = new();
        private readonly Dictionary<int, OwnerComponent> _owners = new();
        private readonly Dictionary<int, ItemDataComponent> _items = new();
        private readonly Dictionary<int, StackComponent> _stacks = new();
        private readonly Dictionary<InventoryStackKey, int> _stackIndex = new();

        public bool AddOrStack(InventoryItemDelta itemDelta)
        {
            ValidateItemDelta(itemDelta);
            if (itemDelta.Amount <= 0)
            {
                return false;
            }

            var stackKey = new InventoryStackKey(itemDelta.OwnerId, itemDelta.ItemId, itemDelta.CategoryId);
            if (_stackIndex.TryGetValue(stackKey, out var existingEntityId))
            {
                var currentStack = _stacks[existingEntityId];
                _stacks[existingEntityId] = new StackComponent(currentStack.Count + itemDelta.Amount);
                return true;
            }

            var entityId = _nextEntityId++;
            _entities.Add(entityId);
            _owners[entityId] = new OwnerComponent(itemDelta.OwnerId);
            _items[entityId] = new ItemDataComponent(itemDelta.ItemId, itemDelta.CategoryId);
            _stacks[entityId] = new StackComponent(itemDelta.Amount);
            _stackIndex[stackKey] = entityId;

            return true;
        }

        public bool Remove(InventoryItemDelta itemDelta)
        {
            ValidateItemDelta(itemDelta);
            if (itemDelta.Amount <= 0)
            {
                return false;
            }

            var stackKey = new InventoryStackKey(itemDelta.OwnerId, itemDelta.ItemId, itemDelta.CategoryId);
            if (!_stackIndex.TryGetValue(stackKey, out var entityId))
            {
                return false;
            }

            var currentStack = _stacks[entityId];
            var remaining = currentStack.Count - itemDelta.Amount;
            if (remaining > 0)
            {
                _stacks[entityId] = new StackComponent(remaining);
                return true;
            }

            _entities.Remove(entityId);
            _owners.Remove(entityId);
            _items.Remove(entityId);
            _stacks.Remove(entityId);
            _stackIndex.Remove(stackKey);
            return true;
        }

        public IReadOnlyList<InventoryItemView> QueryByCategory(string ownerId, string categoryId)
        {
            var result = new List<InventoryItemView>();
            foreach (var entityId in _entities)
            {
                if (!TryBuildItemView(entityId, ownerId, out var view))
                {
                    continue;
                }

                if (!string.Equals(view.CategoryId, categoryId, StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(view);
            }

            return result;
        }

        public IReadOnlyList<InventoryItemView> QueryAll(string ownerId)
        {
            var result = new List<InventoryItemView>();
            foreach (var entityId in _entities)
            {
                if (!TryBuildItemView(entityId, ownerId, out var view))
                {
                    continue;
                }

                result.Add(view);
            }

            return result;
        }

        public void ReplaceOwnerSnapshot(string ownerId, IReadOnlyList<InventoryItemView> items)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                throw new ArgumentException("OwnerId is required.", nameof(ownerId));
            }

            var entitiesToRemove = new List<int>();
            foreach (var entityId in _entities)
            {
                if (_owners.TryGetValue(entityId, out var owner) &&
                    string.Equals(owner.OwnerId, ownerId, StringComparison.Ordinal))
                {
                    entitiesToRemove.Add(entityId);
                }
            }

            foreach (var entityId in entitiesToRemove)
            {
                if (_owners.TryGetValue(entityId, out var ownerComponent) &&
                    _items.TryGetValue(entityId, out var itemComponent))
                {
                    var stackKey = new InventoryStackKey(ownerComponent.OwnerId, itemComponent.ItemId, itemComponent.CategoryId);
                    _stackIndex.Remove(stackKey);
                }

                _entities.Remove(entityId);
                _owners.Remove(entityId);
                _items.Remove(entityId);
                _stacks.Remove(entityId);
            }

            if (items == null || items.Count == 0)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.IsNullOrWhiteSpace(item.ItemId) ||
                    string.IsNullOrWhiteSpace(item.CategoryId) ||
                    item.StackCount <= 0)
                {
                    continue;
                }

                AddOrStack(new InventoryItemDelta(ownerId, item.ItemId, item.StackCount, item.CategoryId));
            }
        }

        private bool TryBuildItemView(int entityId, string ownerId, out InventoryItemView view)
        {
            if (!_owners.TryGetValue(entityId, out var owner) || owner.OwnerId != ownerId)
            {
                view = default;
                return false;
            }

            if (!_items.TryGetValue(entityId, out var itemData))
            {
                view = default;
                return false;
            }

            if (!_stacks.TryGetValue(entityId, out var stack))
            {
                view = default;
                return false;
            }

            view = new InventoryItemView(
                owner.OwnerId,
                itemData.ItemId,
                stack.Count,
                itemData.CategoryId);
            return true;
        }

        private static void ValidateItemDelta(InventoryItemDelta itemDelta)
        {
            if (string.IsNullOrWhiteSpace(itemDelta.OwnerId))
            {
                throw new ArgumentException("OwnerId is required.", nameof(itemDelta));
            }

            if (string.IsNullOrWhiteSpace(itemDelta.ItemId))
            {
                throw new ArgumentException("ItemId is required.", nameof(itemDelta));
            }

            if (string.IsNullOrWhiteSpace(itemDelta.CategoryId))
            {
                throw new ArgumentException("CategoryId is required.", nameof(itemDelta));
            }
        }
    }
}
