using Inventory.API;

namespace Inventory.Implementation.UI
{
    public readonly struct InventoryCategorizedItemUiModel
    {
        public InventoryCategorizedItemUiModel(InventoryItemCategory category, InventoryItemUiModel item)
        {
            Category = category;
            Item = item;
        }

        public InventoryItemCategory Category { get; }
        public InventoryItemUiModel Item { get; }
    }
}
