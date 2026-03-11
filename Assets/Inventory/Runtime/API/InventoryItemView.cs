namespace Inventory.API
{
    public readonly struct InventoryItemView
    {
        public InventoryItemView(
            string ownerId,
            string itemId,
            int stackCount,
            InventoryItemCategory category,
            CardPackMetadata? cardPackMetadata = null)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            StackCount = stackCount;
            Category = category;
            CardPackMetadata = cardPackMetadata;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public int StackCount { get; }
        public InventoryItemCategory Category { get; }
        public CardPackMetadata? CardPackMetadata { get; }
    }
}
