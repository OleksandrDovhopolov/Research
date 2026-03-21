using System.Collections.Generic;

namespace Inventory.API
{
    //TODO add metho remove category and call it from CardCollectionInventoryIntegration
    public interface IItemCategoryRegistry
    {
        void Register(ItemCategory category);
        IReadOnlyList<ItemCategory> GetAllCategories();
        ItemCategory GetById(string categoryId);
    }
}
