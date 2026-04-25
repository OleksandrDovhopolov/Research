using VContainer;

namespace BattlePass
{
    public static class BattlePassInstaller
    {
        public static void RegisterBattlePass(this IContainerBuilder builder)
        {
            builder.Register<IBattlePassRealtimeClock, UnityBattlePassRealtimeClock>(Lifetime.Singleton);
            builder.Register<IBattlePassTimerService, BattlePassTimerService>(Lifetime.Singleton);
            builder.Register<BattlePassUiModelFactory>(Lifetime.Singleton);
            builder.Register<IBattlePassServerService, BattlePassServerService>(Lifetime.Singleton);
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
