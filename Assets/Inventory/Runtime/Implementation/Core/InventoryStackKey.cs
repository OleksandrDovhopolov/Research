using System;
using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal readonly struct InventoryStackKey : IEquatable<InventoryStackKey>
    {
        public InventoryStackKey(string ownerId, string itemId, InventoryItemCategory category)
        {
            OwnerId = ownerId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
            Category = category;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public InventoryItemCategory Category { get; }

        public bool Equals(InventoryStackKey other)
        {
            return OwnerId == other.OwnerId
                   && ItemId == other.ItemId
                   && Category == other.Category;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryStackKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OwnerId, ItemId, (int)Category);
        }
    }
}
