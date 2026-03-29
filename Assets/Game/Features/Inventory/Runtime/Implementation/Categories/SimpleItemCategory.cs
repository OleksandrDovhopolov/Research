using Inventory.API;

namespace Inventory.Implementation
{
    public sealed class SimpleItemCategory : ItemCategory
    {
        public const string Regular = "regular";
        
        public SimpleItemCategory() : base(Regular, "Regular")
        {
        }

        public override CategoryUiMetadata GetMetadata()
        {
            return new ResourceWidgetMetadata();
        }
    }
}
