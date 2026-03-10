namespace Inventory.API
{
    public interface IInventoryCompositionRoot
    {
        IInventoryService CreateInventoryService();
    }
}
