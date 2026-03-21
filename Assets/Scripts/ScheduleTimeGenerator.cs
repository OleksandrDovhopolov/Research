using System;
using System.Collections.Generic;
using EventOrchestration.Models;

namespace core
{
    public static class ScheduleTimeGenerator
    {
        public static ScheduleItem CreateSingleCardCollectionEventStartingIn2Minutes(
            DateTimeOffset nowUtc,
            string eventId = "season_cards_001",
            string streamId = "card_collection_seasons",
            int priority = 10)
        {
            var start = nowUtc.AddMinutes(2);
            var end = start.AddMinutes(10);

            return new ScheduleItem
            {
                Id = eventId,
                EventType = "CardCollection",
                StreamId = streamId,
                StartTimeUtc = start,
                EndTimeUtc = end,
                Priority = priority,
                CustomParams = new Dictionary<string, string>
                {
                    { "eventId", "cards_season_001" },
                },
            };
        }
    }
}
