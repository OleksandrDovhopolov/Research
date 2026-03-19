using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using EventOrchestration.Core;
using EventOrchestration.Infrastructure;
using VContainer;

namespace core
{
    public static class EventOrchestrationVContainerBindings
    {
        public static void RegisterCardCollectionOrchestration(this IContainerBuilder builder, string scheduleJsonFile)
        {
            builder.Register<IScheduleProvider>(_ => new StreamingAssetsScheduleProvider(scheduleJsonFile), Lifetime.Singleton);
            builder.Register<IScheduleValidator, BasicScheduleValidator>(Lifetime.Singleton);
            builder.Register<IConditionProvider, AllowAllConditionProvider>(Lifetime.Singleton);
            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register<IStateStore, InMemoryStateStore>(Lifetime.Singleton);
            builder.Register<IOrchestratorTelemetry, UnityDebugTelemetry>(Lifetime.Singleton);

            builder.Register<ICardCollectionRuntime, CardCollectionDebugRuntime>(Lifetime.Singleton);
            builder.Register<IEventModelFactory, CardCollectionEventModelFactory>(Lifetime.Singleton);
            builder.Register<CardCollectionController>(Lifetime.Singleton);

            builder.Register<IEventRegistry>(resolver =>
            {
                var registry = new EventRegistry();
                var controller = resolver.Resolve<CardCollectionController>();
                registry.Register(controller);
                return registry;
            }, Lifetime.Singleton);

            builder.Register<OrchestratorFactory>(Lifetime.Singleton);
            builder.Register<EventOrchestrator>(
                resolver => resolver.Resolve<OrchestratorFactory>().Create(),
                Lifetime.Singleton);
        }
    }
}
