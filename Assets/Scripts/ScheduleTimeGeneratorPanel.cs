using System;
using System.Collections.Generic;
using System.IO;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    public sealed class ScheduleTimeGeneratorPanel : MonoBehaviour
    {
        [SerializeField] private string _relativeSchedulePathFromAssets = "StreamingAssets/card_collection_schedule.json";
        [SerializeField] private string _eventId = "season_cards_001";
        [SerializeField] private string _streamId = "card_collection_seasons";
        [SerializeField] private int _priority = 10;

        [ContextMenu("Generate Schedule (+2 min / 10 min)")]
        public void GenerateSingleCardCollectionSchedule()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var item = CreateSingleCardCollectionEventStartingIn2Minutes(
                nowUtc,
                _eventId,
                _streamId,
                _priority);

            var schedule = new List<ScheduleItem> { item };
            var settings = new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            };

            var json = JsonConvert.SerializeObject(schedule, Formatting.Indented, settings);
            var normalizedRelativePath = _relativeSchedulePathFromAssets.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Application.dataPath, normalizedRelativePath);
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(fullPath, json);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            Debug.Log($"[ScheduleTimeGeneratorPanel] Schedule generated at '{fullPath}'.");
        }
        
        public ScheduleItem CreateSingleCardCollectionEventStartingIn2Minutes(
            DateTimeOffset nowUtc,
            string eventId = "season_cards_001",
            string streamId = "card_collection_seasons",
            int priority = 10)
        {
            var alignedNowUtc = new DateTimeOffset(
                nowUtc.Year,
                nowUtc.Month,
                nowUtc.Day,
                nowUtc.Hour,
                nowUtc.Minute,
                0,
                TimeSpan.Zero);

            var start = alignedNowUtc.AddMinutes(1);
            var end = start.AddMinutes(3);

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
