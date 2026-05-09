using VContainer;

namespace Game.Crafting
{
    public static class CraftingVContainerBindings
    {
        public static void RegisterCrafting(this IContainerBuilder builder)
        {
            builder.Register<CraftingConfigValidator>(Lifetime.Singleton);
            builder.Register<ICraftingConfigContentSource, StreamingAssetsCraftingConfigContentSource>(Lifetime.Singleton);
            builder.Register<ICraftingConfigProvider, JsonCraftingConfigProvider>(Lifetime.Singleton);
            builder.Register<ICraftingInventoryGateway, SaveBackedCraftingInventoryGateway>(Lifetime.Singleton);
            builder.Register<ICraftingRewardApplier, SaveBackedCraftingRewardApplier>(Lifetime.Singleton);
            builder.Register<ICraftingService, CraftingService>(Lifetime.Singleton);
        }
    }
}
