using Inventory.API;

namespace Inventory.Implementation.UI
{
    public readonly struct InventoryItemUiModel
    {
        public InventoryItemUiModel(
            string itemId,
            string title,
            int stackCount,
            ItemCategory category,
            string subtitle = "")
        {
            ItemId = itemId;
            Title = title;
            StackCount = stackCount;
            Category = category;
            Subtitle = subtitle;
        }

        public string ItemId { get; }
        public string Title { get; }
        public int StackCount { get; }
        public ItemCategory Category { get; }
        public string Subtitle { get; }
    }
}
