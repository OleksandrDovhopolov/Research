using UIShared;
using VContainer;

namespace BattlePass
{
    public static class BattlePassInstaller
    {
        public static void RegisterBattlePass(this IContainerBuilder builder)
        {
            RegisterBattlePassCore(builder);
            RegisterBattlePassPresentation(builder);
            RegisterBattlePassIntegration(builder);
            RegisterBattlePassGameplayReadyIntegration(builder);
            RegisterBattlePassLiveOpsIntegration(builder);
        }

        private static void RegisterBattlePassCore(IContainerBuilder builder)
        {
            builder.Register<IBattlePassPlayerContext, BattlePassPlayerContextAdapter>(Lifetime.Singleton);
            builder.Register<IBattlePassRealtimeClock, UnityBattlePassRealtimeClock>(Lifetime.Singleton);
            builder.Register<IBattlePassTimerService, BattlePassTimerService>(Lifetime.Singleton);
            builder.Register<IBattlePassRewardCatalog, RewardSpecBattlePassRewardCatalog>(Lifetime.Singleton);
            builder.Register<IBattlePassSnapshotStore, BattlePassSnapshotStore>(Lifetime.Singleton);
            builder.Register<IBattlePassXpPresentationTracker, BattlePassXpPresentationTracker>(Lifetime.Singleton);
            builder.Register<IBattlePassStartupSync, BattlePassStartupSync>(Lifetime.Singleton);
            builder.Register<IBattlePassResourceDeltaApplier, ResourceManagerOptimisticResourceApplyService>(Lifetime.Singleton);
            builder.Register<IBattlePassOptimisticRewardApplier, BattlePassOptimisticRewardApplier>(Lifetime.Singleton);
            builder.Register<IBattlePassServerService, BattlePassServerService>(Lifetime.Singleton);
#if DEV_BUILD
            builder.Register<IBattlePassPurchaseVerify, StubBattlePassPurchaseVerify>(Lifetime.Singleton);
#else
            builder.Register<IBattlePassPurchaseVerify, BattlePassPurchaseVerify>(Lifetime.Singleton);
#endif
            builder.Register<IBattlePassPurchaseService>(_ =>
            {
#if DEV_BUILD
                return new MockBattlePassPurchaseService();
#else
                return new UnityIapBattlePassPurchaseService();
#endif
            }, Lifetime.Singleton);
        }

        private static void RegisterBattlePassPresentation(IContainerBuilder builder)
        {
            builder.Register<IBattlePassRewardPresentationCatalog, RewardSpecBattlePassRewardPresentationCatalog>(Lifetime.Singleton);
            builder.Register<BattlePassUiModelFactory>(Lifetime.Singleton);
        }

        private static void RegisterBattlePassIntegration(IContainerBuilder builder)
        {
            builder.Register<IBattlePassWindowRouter, UiManagerBattlePassWindowRouter>(Lifetime.Transient);
            builder.Register<IBattlePassClaimFlowLock, UiManagerBattlePassClaimFlowLock>(Lifetime.Transient);
            builder.Register<IBattlePassPostClaimSync, RewardPlayerStateRefreshBattlePassPostClaimSync>(Lifetime.Transient);
        }

        private static void RegisterBattlePassGameplayReadyIntegration(IContainerBuilder builder)
        {
            builder.Register<BattlePassGameplayReadyInitializer>(Lifetime.Singleton).As<IGameplayReadyInitializer>();
        }

        private static void RegisterBattlePassLiveOpsIntegration(IContainerBuilder builder)
        {
            builder.Register<BattlePassLifecycleState>(Lifetime.Singleton);
            builder.Register<IBattlePassLifecycleState>(resolver => resolver.Resolve<BattlePassLifecycleState>(), Lifetime.Singleton);
            builder.Register<BattlePassEventModelFactory>(Lifetime.Singleton);
            builder.Register<BattlePassLiveOpsController>(Lifetime.Singleton);

            builder.RegisterBuildCallback(container =>
            {
                var eventRegistry = container.Resolve<EventOrchestration.Abstractions.IEventRegistry>();
                var controller = container.Resolve<BattlePassLiveOpsController>();
                eventRegistry.Register(controller);
            });
        }
    }
}
