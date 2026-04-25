using EventOrchestration.Abstractions;
using VContainer;
using VContainer.Unity;

namespace EventOrchestration
{
    public static class EventOrchestrationVContainerBindings
    {
        public static void RegisterOrchestration(this IContainerBuilder builder, string scheduleJsonFile, string scheduleConfigName)
        {
            _ = scheduleConfigName;

            builder.Register<IScheduleValidator, BasicScheduleValidator>(Lifetime.Singleton);
            builder.Register<ILiveOpsScheduleContentSource>(_ => new StreamingAssetsLiveOpsScheduleContentSource(scheduleJsonFile), Lifetime.Singleton);
            builder.Register<IScheduleProvider>(resolver => new JsonScheduleProvider(
                resolver.Resolve<ILiveOpsScheduleContentSource>(),
                resolver.Resolve<IScheduleValidator>()), Lifetime.Singleton);
            builder.Register<IClock, FirebaseClock>(Lifetime.Singleton);
            builder.Register<IStateStore, InMemoryStateStore>(Lifetime.Singleton);
            builder.Register<IOrchestratorTelemetry, UnityDebugTelemetry>(Lifetime.Singleton);
            builder.Register<IEventRegistry, EventRegistry>(Lifetime.Singleton);

            builder.Register<IEventLifecycleEngine, EventLifecycleEngine>(Lifetime.Singleton);
            builder.Register<OrchestratorFactory>(Lifetime.Singleton);
            builder.Register<EventOrchestrator>(resolver => resolver.Resolve<OrchestratorFactory>().Create(), Lifetime.Singleton);
            builder.Register<EventOrchestratorDebugFacade>(Lifetime.Singleton);

            builder.RegisterEntryPoint<EventAssetWarmupService>(Lifetime.Singleton)
                .As<IEventAssetWarmupService>();
        }
    }
}
