using System.Collections.Generic;
using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal sealed class InventoryQuerySystem
    {
        private readonly InventoryWorld _world;

        public InventoryQuerySystem(InventoryWorld world)
        {
            _world = world;
        }

        public IReadOnlyList<InventoryItemView> Execute(string ownerId, string categoryId)
        {
            return _world.QueryByCategory(ownerId, categoryId);
        }

        public IReadOnlyList<InventoryItemView> ExecuteAll(string ownerId)
        {
            return _world.QueryAll(ownerId);
        }
    }
}
