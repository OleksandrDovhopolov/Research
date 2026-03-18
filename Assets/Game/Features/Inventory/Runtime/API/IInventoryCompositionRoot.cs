namespace Inventory.API
{
    public interface IInventoryCompositionRoot
    {
        void AddUseHandler(IInventoryItemUseHandler handler);
        void RemoveUseHandler(IInventoryItemUseHandler handler);
        IItemCategoryRegistry GetCategoryRegistry();
        IInventoryService CreateInventoryService();
        IInventoryReadService CreateInventoryReadService();
        IInventoryItemUseService CreateInventoryItemUseService();
    }
}
