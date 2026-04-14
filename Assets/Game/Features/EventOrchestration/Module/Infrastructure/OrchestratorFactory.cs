using EventOrchestration.Abstractions;

namespace EventOrchestration
{
    public sealed class OrchestratorFactory
    {
        private readonly IScheduleProvider _scheduleProvider;
        private readonly IScheduleValidator _scheduleValidator;
        private readonly IClock _clock;
        private readonly IStateStore _stateStore;
        private readonly IEventLifecycleEngine _engine;

        public OrchestratorFactory(
            IScheduleProvider scheduleProvider,
            IScheduleValidator scheduleValidator,
            IClock clock,
            IStateStore stateStore,
            IEventLifecycleEngine engine)
        {
            _scheduleProvider = scheduleProvider;
            _scheduleValidator = scheduleValidator;
            _clock = clock;
            _stateStore = stateStore;
            _engine = engine;
        }

        public EventOrchestrator Create()
        {
            return new EventOrchestrator(
                _scheduleProvider,
                _scheduleValidator,
                _clock,
                _stateStore,
                _engine);
        }
    }
}
