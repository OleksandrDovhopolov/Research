using System;
using System.Collections.Generic;

namespace EventOrchestration.Models
{
    [Serializable]
    public sealed class ScheduleItem
    {
        public string Id;
        public string EventType;
        public DateTimeOffset StartTimeUtc;
        public DateTimeOffset EndTimeUtc;
        public int Priority;
        public string StreamId;
        public Dictionary<string, string> CustomParams;
    }
}
