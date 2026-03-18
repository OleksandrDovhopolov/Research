using System.Collections.Generic;

namespace Inventory.API
{
    public interface IItemCategoryRegistry
    {
        void Register(ItemCategory category);
        IReadOnlyList<ItemCategory> GetAllCategories();
        ItemCategory GetById(string categoryId);
    }
}
