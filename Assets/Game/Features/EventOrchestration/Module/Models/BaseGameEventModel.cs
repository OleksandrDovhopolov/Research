namespace EventOrchestration.Models
{
    public abstract class BaseGameEventModel
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public string StreamId { get; set; }
        public string CollectionName { get; set; }
    }
}
