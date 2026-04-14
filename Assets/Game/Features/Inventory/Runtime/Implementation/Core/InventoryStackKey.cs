using System;

namespace Inventory.Implementation.Core
{
    internal readonly struct InventoryStackKey : IEquatable<InventoryStackKey>
    {
        public InventoryStackKey(string ownerId, string itemId, string categoryId)
        {
            OwnerId = ownerId ?? string.Empty;
            ItemId = itemId ?? string.Empty;
            CategoryId = categoryId ?? string.Empty;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public string CategoryId { get; }

        public bool Equals(InventoryStackKey other)
        {
            return OwnerId == other.OwnerId
                   && ItemId == other.ItemId
                   && CategoryId == other.CategoryId;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryStackKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(OwnerId, ItemId, CategoryId);
        }
    }
}
