using VContainer;

namespace Game.Fishing
{
    public static class FishingVContainerBindings
    {
        public static void RegisterFishing(this IContainerBuilder builder)
        {
            builder.Register<FishingConfigValidator>(Lifetime.Singleton);
            builder.Register<IFishingConfigContentSource, StreamingAssetsFishingConfigContentSource>(Lifetime.Singleton);
            builder.Register<IFishingConfigProvider, JsonFishingConfigProvider>(Lifetime.Singleton);

            builder.Register<IFishingRandom, SystemFishingRandom>(Lifetime.Singleton);
            builder.Register<IFishSelector, FishSelector>(Lifetime.Singleton);
            builder.Register<IFishWeightService, FishWeightService>(Lifetime.Singleton);
            builder.Register<IActiveFishingEventsProvider, EmptyActiveFishingEventsProvider>(Lifetime.Singleton);
            builder.Register<IFishingInventoryGateway, SaveBackedFishingInventoryGateway>(Lifetime.Singleton);
            builder.Register<IFishBookService, SaveBackedFishBookService>(Lifetime.Singleton);
            builder.Register<IFishCollectionDataBuilder, FishCollectionDataBuilder>(Lifetime.Singleton);
            builder.Register<IFishingService, FishingService>(Lifetime.Singleton);
            builder.Register<IFishingZoneInfoLogger, FishingZoneInfoLogger>(Lifetime.Singleton);
        }
    }
}
