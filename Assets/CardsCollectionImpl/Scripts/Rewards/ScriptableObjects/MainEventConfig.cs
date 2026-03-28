using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    [CreateAssetMenu(fileName = "MainEventConfig", menuName = "Configs/MainEventConfig")]
    public class MainEventConfig : ScriptableObject
    {
        [Header("EventsSettings")] public List<EventDataEntry> Events = new List<EventDataEntry>();

        [ContextMenu("Validate All Events")]
        public void ValidateAll()
        {
            var hasErrors = false;
            foreach (var ev in Events)
            {
                if (ValidateEvent(ev))
                {
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Debug.LogWarning("<color=green>[Validation]</color> No errors found.");
            }
        }

        private bool ValidateEvent(EventDataEntry entry)
        {
            if (entry.GroupsJson == null || entry.RewardsConfig == null)
            {
                Debug.LogWarning($"[Validation] Event {entry.EventName}: Not all files are assigned.");
                return true;
            }

            var jsonContent = JsonUtility.FromJson<GroupsRoot>(entry.GroupsJson.text);
            var jsonIds = jsonContent.groups.Select(g => g.groupType).ToHashSet();

            var soIds = entry.RewardsConfig.GroupRewards.Select(r => r.GroupId).ToHashSet();
            var hasErrors = false;

            foreach (var id in jsonIds)
            {
                if (!soIds.Contains(id))
                {
                    Debug.LogError(
                        $"<color=red>[Error {entry.EventName}]</color> Group ID '{id}' exists in JSON, but has no reward in SO.");
                    hasErrors = true;
                }
            }

            foreach (var id in soIds)
            {
                if (!jsonIds.Contains(id))
                {
                    Debug.LogError(
                        $"<color=orange>[Error {entry.EventName}]</color> SO has a reward for group ID '{id}', but this group does not exist in JSON.");
                    hasErrors = true;
                }
            }

            return hasErrors;
        }

        private void OnValidate()
        {
            ValidateAll();
        }
    }

    [Serializable]
    public class EventDataEntry
    {
        public string EventName;
        public TextAsset GroupsJson;
        public CardCollectionRewardsConfigSO RewardsConfig;
    }
}