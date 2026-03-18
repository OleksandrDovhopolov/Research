namespace Inventory.Implementation.Core
{
    internal readonly struct ItemDataComponent
    {
        public ItemDataComponent(string itemId, string categoryId)
        {
            ItemId = itemId;
            CategoryId = categoryId;
        }

        public string ItemId { get; }
        public string CategoryId { get; }
    }

    internal readonly struct OwnerComponent
    {
        public OwnerComponent(string ownerId)
        {
            OwnerId = ownerId;
        }

        public string OwnerId { get; }
    }

    internal readonly struct StackComponent
    {
        public StackComponent(int count)
        {
            Count = count;
        }

        public int Count { get; }
    }

}
