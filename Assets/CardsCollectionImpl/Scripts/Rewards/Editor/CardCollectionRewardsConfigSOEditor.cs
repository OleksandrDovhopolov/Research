using CardCollection.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl.Editor
{
    [CustomEditor(typeof(CardCollectionRewardsConfigSO))]
    public class CardCollectionRewardsConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Group Rewards"))
            {
                ClearRewards((CardCollectionRewardsConfigSO)target);
            }

            if (GUILayout.Button("Sync Group Rewards From GroupsJson"))
            {
                SyncRewards((CardCollectionRewardsConfigSO)target);
            }
        }

        private static void ClearRewards(CardCollectionRewardsConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            Undo.RecordObject(config, "Clear Card Collection Group Rewards");
            config.GroupRewards = new CollectionCompletionRewardConfig[0];
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SyncRewards(CardCollectionRewardsConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            if (config.GroupsJson == null)
            {
                Debug.LogWarning("[CardCollectionRewardsConfigSOEditor] GroupsJson is not assigned.");
                return;
            }

            var root = JsonUtility.FromJson<GroupsRoot>(config.GroupsJson.text);
            if (root?.groups == null || root.groups.Count == 0)
            {
                Debug.LogWarning("[CardCollectionRewardsConfigSOEditor] GroupsJson has no groups.");
                return;
            }

            var existingByGroupId = new Dictionary<string, CollectionCompletionRewardConfig>();
            if (config.GroupRewards != null)
            {
                foreach (var existing in config.GroupRewards)
                {
                    if (!string.IsNullOrWhiteSpace(existing.GroupId))
                    {
                        existingByGroupId[existing.GroupId] = existing;
                    }
                }
            }

            var uniqueGroupTypes = root.groups
                .Select(g => g.groupType)
                .Where(groupType => !string.IsNullOrWhiteSpace(groupType))
                .Distinct()
                .ToArray();

            var rewards = new CollectionCompletionRewardConfig[uniqueGroupTypes.Length];
            for (var i = 0; i < uniqueGroupTypes.Length; i++)
            {
                var groupType = uniqueGroupTypes[i];
                if (existingByGroupId.TryGetValue(groupType, out var existingReward))
                {
                    rewards[i] = existingReward;
                    continue;
                }

                rewards[i] = new CollectionCompletionRewardConfig
                {
                    GroupId = groupType,
                    RewardId = string.Empty,
                    Amount = 0,
                    Icon = null
                };
            }

            Undo.RecordObject(config, "Sync Card Collection Group Rewards");
            config.GroupRewards = rewards;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
