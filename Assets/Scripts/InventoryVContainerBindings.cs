using Inventory.API;
using Inventory.Implementation;
using VContainer;

namespace core
{
    public static class InventoryVContainerBindings
    {
        public static void RegisterInventoryService(this IContainerBuilder builder)
        {
            builder.Register<IInventoryService>(_ =>
            {
                if (!InventoryCompositionRegistry.IsRegistered)
                {
                    InventoryCompositionRegistry.Register(new InventoryImplementationCompositionRoot());
                }

                return InventoryCompositionRegistry.Resolve().CreateInventoryService();
            }, Lifetime.Singleton);
        }
    }
}
