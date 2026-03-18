using System.Collections.Generic;
using Inventory.API;
using R3;

namespace Inventory.Implementation.UI
{
    public sealed class InventoryCategoryTabViewModel
    {
        public InventoryCategoryTabViewModel(ItemCategory category)
        {
            Category = category;
        }

        public ItemCategory Category { get; }

        public ReactiveProperty<IReadOnlyList<InventoryItemUiModel>> Items { get; } =
            new(new List<InventoryItemUiModel>());
    }
}
