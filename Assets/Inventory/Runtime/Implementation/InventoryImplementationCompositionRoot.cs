using Inventory.API;
using Inventory.Implementation.Services;

namespace Inventory.Implementation
{
    public sealed class InventoryImplementationCompositionRoot : IInventoryCompositionRoot
    {
        private IInventoryService _inventoryService;

        public IInventoryService CreateInventoryService()
        {
            _inventoryService ??= new InventoryModuleService();
            return _inventoryService;
        }
    }
}
