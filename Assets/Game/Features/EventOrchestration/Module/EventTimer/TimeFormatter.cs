using System;

namespace EventOrchestration
{
    public static class TimeFormatter
    {
        public static string Format(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{ts.Days}d {ts.Hours}h";

            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
