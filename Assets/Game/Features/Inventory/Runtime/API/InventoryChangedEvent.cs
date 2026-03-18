using System;
using System.Collections.Generic;

namespace Inventory.API
{
    public readonly struct InventoryChangedEvent
    {
        public InventoryChangedEvent(
            string ownerId,
            IReadOnlyDictionary<string, IReadOnlyList<InventoryItemView>> itemsByCategory,
            DateTime changedAtUtc)
        {
            OwnerId = ownerId;
            ItemsByCategory = itemsByCategory;
            ChangedAtUtc = changedAtUtc;
        }

        public string OwnerId { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<InventoryItemView>> ItemsByCategory { get; }
        public DateTime ChangedAtUtc { get; }
    }
}
