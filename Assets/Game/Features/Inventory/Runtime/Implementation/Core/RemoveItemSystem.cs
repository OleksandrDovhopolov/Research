using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal sealed class RemoveItemSystem
    {
        private readonly InventoryWorld _world;

        public RemoveItemSystem(InventoryWorld world)
        {
            _world = world;
        }

        public bool Execute(InventoryItemDelta itemDelta)
        {
            return _world.Remove(itemDelta);
        }
    }
}
