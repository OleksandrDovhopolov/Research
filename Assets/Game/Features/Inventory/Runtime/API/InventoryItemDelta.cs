namespace Inventory.API
{
    public readonly struct InventoryItemDelta
    {
        public InventoryItemDelta(
            string ownerId,
            string itemId,
            int amount,
            string categoryId)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            Amount = amount;
            CategoryId = categoryId;
        }

        public InventoryItemDelta(
            string ownerId,
            string itemId,
            int amount,
            ItemCategory category)
            : this(ownerId, itemId, amount, category?.CategoryId ?? string.Empty)
        {
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public int Amount { get; }
        public string CategoryId { get; }
    }
}
