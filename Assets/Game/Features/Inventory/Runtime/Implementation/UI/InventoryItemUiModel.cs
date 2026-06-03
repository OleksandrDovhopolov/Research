using Inventory.API;
using UnityEngine;

namespace Inventory.Implementation.UI
{
    public readonly struct InventoryItemUiModel
    {
        public InventoryItemUiModel(
            string itemId,
            string title,
            int stackCount,
            ItemCategory category,
            string subtitle = "",
            Sprite icon = null)
        {
            ItemId = itemId;
            Title = title;
            StackCount = stackCount;
            Category = category;
            Subtitle = subtitle;
            Icon = icon;
        }

        public string ItemId { get; }
        public string Title { get; }
        public int StackCount { get; }
        public ItemCategory Category { get; }
        public string Subtitle { get; }

        /// <summary>
        /// Pre-resolved icon (e.g. from RewardSpec). When null, the view falls back
        /// to its own Addressables-based loader.
        /// </summary>
        public Sprite Icon { get; }
    }
}
