using Inventory.API;
using Inventory.Implementation;
using VContainer;

namespace core
{
    public static class InventoryVContainerBindings
    {
        public const string InventoryOwnerId = "player_1";

        public static void RegisterInventoryService(this IContainerBuilder builder)
        {
            builder.RegisterInstance(InventoryOwnerId);

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
