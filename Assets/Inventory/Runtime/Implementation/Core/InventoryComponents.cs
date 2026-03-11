using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal readonly struct ItemDataComponent
    {
        public ItemDataComponent(string itemId, InventoryItemCategory category)
        {
            ItemId = itemId;
            Category = category;
        }

        public string ItemId { get; }
        public InventoryItemCategory Category { get; }
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

    internal readonly struct CardPackComponent
    {
        public CardPackComponent(string packName, int cardsInside)
        {
            PackName = packName;
            CardsInside = cardsInside;
        }

        public string PackName { get; }
        public int CardsInside { get; }
    }
}
