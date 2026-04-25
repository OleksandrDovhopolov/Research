using System;
using System.Collections.Generic;
using System.IO;
using EventOrchestration.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace EventOrchestration
{
    public sealed class ScheduleTimeGeneratorPanel : MonoBehaviour
    {
        private const string EventIdPrefix = "season_cards_";
        private const string RewardsPrefix = "season_rewards_";
        private const string GroupsPrefix = "season_groups_";
        private const string PacksPrefix = "season_packs_";
        
        [SerializeField] private string _relativeSchedulePathFromAssets = "StreamingAssets/liveops_schedule.json";
        [SerializeField] private int _eventIndex = 1;
        [SerializeField] private int _minDelay = 1;
        [SerializeField] private int _minDuration = 3;
        [SerializeField] private string _streamId = "card_collection_seasons";
        [SerializeField] private int _priority = 10;

        [ContextMenu("Generate Schedule (+2 min / 10 min)")]
        public void GenerateSingleCardCollectionSchedule()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var eventId = BuildEventId();
            var item = CreateSingleCardCollectionEventStartingIn2Minutes(
                nowUtc,
                eventId,
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
            IncrementEventId();
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

            var start = alignedNowUtc.AddMinutes(_minDelay);
            var end = start.AddMinutes(_minDuration);

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
                    { "eventId", eventId},
                    { "rewardsConfigAddress", BuildAddress(RewardsPrefix)},
                    { "cardsCollectionAddress", eventId},
                    { "cardGroupsAddress", BuildAddress(GroupsPrefix)},
                    { "cardPacksAddress", "shared_card_packs_config"},
                },
            };
        }

        private string BuildEventId()
        {
            return BuildAddress(EventIdPrefix);
        }

        private string BuildAddress(string prefix)
        {
            return $"{prefix}{_eventIndex:D3}";
        }

        private void IncrementEventId()
        {
            if (_eventIndex < 1)
            {
                _eventIndex = 1;
            }

            _eventIndex += 1;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
