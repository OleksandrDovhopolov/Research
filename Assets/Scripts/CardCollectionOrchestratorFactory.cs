using EventOrchestration.Abstractions;
using EventOrchestration.Core;

namespace core
{
    public sealed class CardCollectionOrchestratorFactory
    {
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IEventRegistry _eventRegistry;
        private readonly IConditionProvider _conditionProvider;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IOrchestratorTelemetry _telemetry;

        public CardCollectionOrchestratorFactory(
            IScheduleProvider scheduleProvider,
            IScheduleValidator scheduleValidator,
            IEventRegistry eventRegistry,
            IConditionProvider conditionProvider,
            IClock clock,
            IStateStore stateStore,
            IOrchestratorTelemetry telemetry)
        {
            _scheduleProvider = scheduleProvider;
            _scheduleValidator = scheduleValidator;
            _eventRegistry = eventRegistry;
            _conditionProvider = conditionProvider;
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
                _conditionProvider,
                _clock,
                _stateStore,
                _telemetry);
        }
    }
}
