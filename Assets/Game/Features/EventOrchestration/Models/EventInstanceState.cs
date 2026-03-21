namespace EventOrchestration.Models
{
    public enum EventInstanceState
    {
        Unknown = 0,
        Pending = 1,
        Starting = 2,
        Active = 3,
        Ending = 4,
        Settling = 5,
        Completed = 6,
        Failed = 7,
        Cancelled = 8,
    }
}
