using EventOrchestration.Abstractions;
using EventOrchestration.Core;
using EventOrchestration.Infrastructure;
using VContainer;

namespace core
{
    public static class EventOrchestrationVContainerBindings
    {
        public static void RegisterOrchestration(this IContainerBuilder builder, string scheduleJsonFile)
        {
            builder.Register<IScheduleProvider>(_ => new StreamingAssetsScheduleProvider(scheduleJsonFile), Lifetime.Singleton);
            builder.Register<IScheduleValidator, BasicScheduleValidator>(Lifetime.Singleton);
            builder.Register<IConditionProvider, AllowAllConditionProvider>(Lifetime.Singleton);
            builder.Register<IClock, SystemClock>(Lifetime.Singleton);
            builder.Register<IStateStore, InMemoryStateStore>(Lifetime.Singleton);
            builder.Register<IOrchestratorTelemetry, UnityDebugTelemetry>(Lifetime.Singleton);
            builder.Register<IEventRegistry, EventRegistry>(Lifetime.Singleton);

            builder.Register<OrchestratorFactory>(Lifetime.Singleton);
            builder.Register<EventOrchestrator>(
                resolver => resolver.Resolve<OrchestratorFactory>().Create(),
                Lifetime.Singleton);
        }
    }
}
