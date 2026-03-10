namespace Inventory.API
{
    public readonly struct InventoryItemView
    {
        public InventoryItemView(
            string ownerId,
            string itemId,
            string itemType,
            int stackCount,
            InventoryItemCategory category,
            CardPackMetadata? cardPackMetadata = null)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            ItemType = itemType;
            StackCount = stackCount;
            Category = category;
            CardPackMetadata = cardPackMetadata;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public string ItemType { get; }
        public int StackCount { get; }
        public InventoryItemCategory Category { get; }
        public CardPackMetadata? CardPackMetadata { get; }
    }
}
