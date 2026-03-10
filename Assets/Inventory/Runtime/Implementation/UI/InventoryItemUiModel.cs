namespace Inventory.Implementation.UI
{
    public readonly struct InventoryItemUiModel
    {
        public InventoryItemUiModel(string itemId, string title, int stackCount, string subtitle = "")
        {
            ItemId = itemId;
            Title = title;
            StackCount = stackCount;
            Subtitle = subtitle;
        }

        public string ItemId { get; }
        public string Title { get; }
        public int StackCount { get; }
        public string Subtitle { get; }
    }
}
