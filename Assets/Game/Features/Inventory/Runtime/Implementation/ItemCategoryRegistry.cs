using System;
using System.Collections.Generic;
using Inventory.API;

namespace Inventory.Implementation
{
    public sealed class ItemCategoryRegistry : IItemCategoryRegistry
    {
        private readonly Dictionary<string, ItemCategory> _categoriesById = new(StringComparer.Ordinal);
        
        public IReadOnlyList<ItemCategory> GetAllCategories()
        {
            return new List<ItemCategory>(_categoriesById.Values);
        }

        public ItemCategory GetById(string categoryId)
        {
            return string.IsNullOrWhiteSpace(categoryId) || !_categoriesById.TryGetValue(categoryId, out var category)
                ? null
                : category;
        }

        public void Register(ItemCategory category)
        {
            if (category == null || string.IsNullOrWhiteSpace(category.CategoryId))
            {
                return;
            }

            _categoriesById[category.CategoryId] = category;
        }
    }
}
