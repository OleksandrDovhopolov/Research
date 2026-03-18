namespace Inventory.API
{
    public readonly struct InventoryItemView
    {
        public InventoryItemView(
            string ownerId,
            string itemId,
            int stackCount,
            string categoryId)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            StackCount = stackCount;
            CategoryId = categoryId;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public int StackCount { get; }
        public string CategoryId { get; }
    }
}
