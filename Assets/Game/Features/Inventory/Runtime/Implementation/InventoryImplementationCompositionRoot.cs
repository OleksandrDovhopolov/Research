using Inventory.API;
using Inventory.Implementation.Services;

namespace Inventory.Implementation
{
    public sealed class InventoryImplementationCompositionRoot : IInventoryCompositionRoot
    {
        private InventoryModuleService _inventoryService;
        
        private readonly IInventoryStorage _inventoryStorage;
        private readonly IItemCategoryRegistry _categoryRegistry;

        public InventoryImplementationCompositionRoot()
        {
            _categoryRegistry = new ItemCategoryRegistry();
            _categoryRegistry.Register(new SimpleItemCategory());
            
            _inventoryStorage = new InMemoryInventoryStorage();
        }
        
        public void AddUseHandler(IInventoryItemUseHandler handler) 
        {
            CreateOrGetInventoryModuleService().AddUseHandler(handler);
        }

        public void RemoveUseHandler(IInventoryItemUseHandler handler)
        {
            CreateOrGetInventoryModuleService().RemoveUseHandler(handler);
        }

        public IItemCategoryRegistry GetCategoryRegistry()
        {
            return _categoryRegistry;
        }
        
        public IInventoryService CreateInventoryService()
        {
            return CreateOrGetInventoryModuleService();
        }

        public IInventoryReadService CreateInventoryReadService()
        {
            return CreateOrGetInventoryModuleService();
        }

        public IInventoryItemUseService CreateInventoryItemUseService()
        {
             return CreateOrGetInventoryModuleService();
        }
        
        private InventoryModuleService CreateOrGetInventoryModuleService()
        {
            _inventoryService ??= new InventoryModuleService(_inventoryStorage);
            return _inventoryService;
        }
    }
}
