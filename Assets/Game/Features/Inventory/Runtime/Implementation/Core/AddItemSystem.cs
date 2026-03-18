using Inventory.API;

namespace Inventory.Implementation.Core
{
    internal sealed class AddItemSystem
    {
        private readonly InventoryWorld _world;

        public AddItemSystem(InventoryWorld world)
        {
            _world = world;
        }

        public bool Execute(InventoryItemDelta itemDelta)
        {
            return _world.AddOrStack(itemDelta);
        }
    }
}
