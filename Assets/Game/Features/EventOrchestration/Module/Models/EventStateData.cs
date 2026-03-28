using System;

namespace EventOrchestration.Models
{
    [Serializable]
    public sealed class EventStateData
    {
        public string ScheduleItemId;
        public EventInstanceState State;
        public int Version;
        public DateTimeOffset UpdatedAtUtc;
        public string LastError;
        public bool StartInvoked;
        public bool EndInvoked;
        public bool SettlementInvoked;
    }
}
