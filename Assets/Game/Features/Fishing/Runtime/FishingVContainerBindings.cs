using VContainer;
using VContainer.Unity;

namespace Game.Fishing
{
    public static class FishingInstaller
    {
        public static void RegisterFishing(this IContainerBuilder builder)
        {
            RegisterFishingCore(builder);
            RegisterFishingPresentation(builder);
            RegisterFishingRuntimeIntegration(builder);
        }

        private static void RegisterFishingCore(IContainerBuilder builder)
        {
            builder.Register<FishingConfigValidator>(Lifetime.Singleton);
            builder.Register<IFishingConfigContentSource, StreamingAssetsFishingConfigContentSource>(Lifetime.Singleton);
            builder.Register<IFishingConfigProvider, JsonFishingConfigProvider>(Lifetime.Singleton);

            builder.Register<IFishingRandom, SystemFishingRandom>(Lifetime.Singleton);
            builder.Register<IFishSelector, FishSelector>(Lifetime.Singleton);
            builder.Register<IFishWeightService, FishWeightService>(Lifetime.Singleton);
            builder.Register<IFishCatchResolver, ConfigBackedFishCatchResolver>(Lifetime.Singleton);
            builder.Register<ICaughtFishPresenter, LoggingCaughtFishPresenter>(Lifetime.Singleton);
            builder.Register<ICaughtFishService, CaughtFishService>(Lifetime.Singleton);
            builder.Register<IActiveFishingEventsProvider, EmptyActiveFishingEventsProvider>(Lifetime.Singleton);
            builder.Register<IFishingInventoryGateway, SaveBackedFishingInventoryGateway>(Lifetime.Singleton);
            builder.Register<IFishBookService, SaveBackedFishBookService>(Lifetime.Singleton);
            builder.Register<IFishingService, FishingService>(Lifetime.Singleton);
        }

        private static void RegisterFishingPresentation(IContainerBuilder builder)
        {
            builder.Register<IFishCollectionDataBuilder, FishCollectionDataBuilder>(Lifetime.Singleton);
            builder.Register<IFishingHudLureDataBuilder, FishingHudLureDataBuilder>(Lifetime.Singleton);
            builder.Register<IFishingMinigameFacade, FishingMinigameFacade>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FishingHudFacade>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FishingLureSelectionHudFacade>(Lifetime.Singleton);
        }

        private static void RegisterFishingRuntimeIntegration(IContainerBuilder builder)
        {
            builder.Register<IFishingLureSelectionStartService, FishingLureSelectionStartService>(Lifetime.Singleton);
        }
    }
}
