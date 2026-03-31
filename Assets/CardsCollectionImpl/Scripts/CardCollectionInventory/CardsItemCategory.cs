using Inventory.API;

namespace CardCollectionImpl
{
    public sealed class CardsItemCategory : ItemCategory
    {
        public CardsItemCategory(string categoryId) : base(categoryId, "Card packs")
        {
        }

        public override CategoryUiMetadata GetMetadata()
        {
            return new ActionWidgetMetadata();
        }
    }
}
