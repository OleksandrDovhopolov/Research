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

        /*public override string ToString()
        {
            var customParamsStr = CustomParams == null
                ? "null"
                : string.Join(", ", CustomParams.Select(kv => $"{kv.Key}={kv.Value}"));

            return $"Id={Id}, " +
                   //$"EventType={EventType}, " +
                   $"StartTimeUtc={StartTimeUtc:O}, " +
                   $"EndTimeUtc={EndTimeUtc:O}";
                   //$"Priority={Priority}, " +
                  // $"StreamId={StreamId}, " +
                   //$"CustomParams={{ {customParamsStr} }}";
        }*/
    }
}
