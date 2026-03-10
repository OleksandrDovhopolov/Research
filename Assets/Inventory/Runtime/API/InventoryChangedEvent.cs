using System;
using System.Collections.Generic;

namespace Inventory.API
{
    public readonly struct InventoryChangedEvent
    {
        public InventoryChangedEvent(
            string ownerId,
            IReadOnlyList<InventoryItemView> regularItems,
            IReadOnlyList<InventoryItemView> cardPacks,
            DateTime changedAtUtc)
        {
            OwnerId = ownerId;
            RegularItems = regularItems;
            CardPacks = cardPacks;
            ChangedAtUtc = changedAtUtc;
        }

        public string OwnerId { get; }
        public IReadOnlyList<InventoryItemView> RegularItems { get; }
        public IReadOnlyList<InventoryItemView> CardPacks { get; }
        public DateTime ChangedAtUtc { get; }
    }
}
