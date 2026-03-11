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
        private readonly Dictionary<int, CardPackComponent> _cardPacks = new();
        private readonly Dictionary<InventoryStackKey, int> _stackIndex = new();

        public bool AddOrStack(InventoryItemDelta itemDelta)
        {
            ValidateItemDelta(itemDelta);
            if (itemDelta.Amount <= 0)
            {
                return false;
            }

            var stackKey = new InventoryStackKey(itemDelta.OwnerId, itemDelta.ItemId, itemDelta.Category);
            if (_stackIndex.TryGetValue(stackKey, out var existingEntityId))
            {
                var currentStack = _stacks[existingEntityId];
                _stacks[existingEntityId] = new StackComponent(currentStack.Count + itemDelta.Amount);
                return true;
            }

            var entityId = _nextEntityId++;
            _entities.Add(entityId);
            _owners[entityId] = new OwnerComponent(itemDelta.OwnerId);
            _items[entityId] = new ItemDataComponent(itemDelta.ItemId, itemDelta.Category);
            _stacks[entityId] = new StackComponent(itemDelta.Amount);
            _stackIndex[stackKey] = entityId;

            if (itemDelta.Category == InventoryItemCategory.CardPack && itemDelta.CardPackMetadata.HasValue)
            {
                var metadata = itemDelta.CardPackMetadata.Value;
                _cardPacks[entityId] = new CardPackComponent(metadata.PackName, metadata.CardsInside);
            }

            return true;
        }

        public bool Remove(InventoryItemDelta itemDelta)
        {
            ValidateItemDelta(itemDelta);
            if (itemDelta.Amount <= 0)
            {
                return false;
            }

            var stackKey = new InventoryStackKey(itemDelta.OwnerId, itemDelta.ItemId, itemDelta.Category);
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
            _cardPacks.Remove(entityId);
            _stackIndex.Remove(stackKey);
            return true;
        }

        public IReadOnlyList<InventoryItemView> QueryByCategory(string ownerId, InventoryItemCategory category)
        {
            var result = new List<InventoryItemView>();
            foreach (var entityId in _entities)
            {
                if (!_owners.TryGetValue(entityId, out var owner) || owner.OwnerId != ownerId)
                {
                    continue;
                }

                if (!_items.TryGetValue(entityId, out var itemData) || itemData.Category != category)
                {
                    continue;
                }

                if (!_stacks.TryGetValue(entityId, out var stack))
                {
                    continue;
                }

                CardPackMetadata? metadata = null;
                if (_cardPacks.TryGetValue(entityId, out var cardPack))
                {
                    metadata = new CardPackMetadata(cardPack.PackName, cardPack.CardsInside);
                }

                result.Add(new InventoryItemView(
                    owner.OwnerId,
                    itemData.ItemId,
                    stack.Count,
                    itemData.Category,
                    metadata));
            }

            return result;
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
        }
    }
}
