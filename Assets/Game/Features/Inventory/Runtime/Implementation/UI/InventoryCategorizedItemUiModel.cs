namespace Inventory.Implementation.UI
{
    public readonly struct InventoryCategorizedItemUiModel
    {
        public InventoryCategorizedItemUiModel(string categoryId, InventoryItemUiModel item)
        {
            CategoryId = categoryId;
            Item = item;
        }

        public string CategoryId { get; }
        public InventoryItemUiModel Item { get; }
    }
}
