namespace Inventory.API
{
    public interface IInventoryUseHandlerStorage
    {
        void AddUseHandler(IInventoryItemUseHandler handler);
        void RemoveUseHandler(IInventoryItemUseHandler handler);
    }
}
