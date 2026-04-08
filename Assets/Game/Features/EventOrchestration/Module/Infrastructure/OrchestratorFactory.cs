using EventOrchestration.Abstractions;

namespace EventOrchestration
{
    public sealed class OrchestratorFactory
    {
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IEventRegistry _eventRegistry;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IOrchestratorTelemetry _telemetry;

        public OrchestratorFactory(
            IScheduleProvider scheduleProvider,
            IScheduleValidator scheduleValidator,
            IEventRegistry eventRegistry,
            IClock clock,
            IStateStore stateStore,
            IOrchestratorTelemetry telemetry)
        {
            _scheduleProvider = scheduleProvider;
            _scheduleValidator = scheduleValidator;
            _eventRegistry = eventRegistry;
            _clock = clock;
            _stateStore = stateStore;
            _telemetry = telemetry;
        }

        public EventOrchestrator Create()
        {
            return new EventOrchestrator(
                _scheduleProvider,
                _scheduleValidator,
                _eventRegistry,
                _clock,
                _stateStore,
                _telemetry);
        }
    }
}
