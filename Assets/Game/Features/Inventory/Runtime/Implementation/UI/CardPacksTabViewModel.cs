using System.Collections.Generic;
using R3;

namespace Inventory.Implementation.UI
{
    public sealed class CardPacksTabViewModel
    {
        public ReactiveProperty<IReadOnlyList<InventoryItemUiModel>> Items { get; } =
            new(new List<InventoryItemUiModel>());
    }
}
