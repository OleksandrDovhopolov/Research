namespace EventOrchestration.Abstractions
{
    public interface IEventRegistry
    {
        void Register(IEventController controller);
        bool TryGet(string eventType, out IEventController controller);
    }
}
