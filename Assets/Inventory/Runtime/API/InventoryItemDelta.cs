namespace Inventory.API
{
    public readonly struct InventoryItemDelta
    {
        public InventoryItemDelta(
            string ownerId,
            string itemId,
            string itemType,
            int amount,
            InventoryItemCategory category,
            CardPackMetadata? cardPackMetadata = null)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            ItemType = itemType;
            Amount = amount;
            Category = category;
            CardPackMetadata = cardPackMetadata;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public string ItemType { get; }
        public int Amount { get; }
        public InventoryItemCategory Category { get; }
        public CardPackMetadata? CardPackMetadata { get; }
    }
}
