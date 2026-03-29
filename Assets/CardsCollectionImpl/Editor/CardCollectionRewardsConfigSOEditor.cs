using CardCollection.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl.Editor
{
    [CustomEditor(typeof(CardCollectionRewardsConfigSO))]
    [CanEditMultipleObjects]
    public class CardCollectionRewardsConfigSOEditor : UnityEditor.Editor
    {
        private CardCollectionRewardsConfigSO _linkedConfig;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            _linkedConfig = (CardCollectionRewardsConfigSO)EditorGUILayout.ObjectField(
                "Linked Config",
                _linkedConfig,
                typeof(CardCollectionRewardsConfigSO),
                false);

            if (GUILayout.Button("Copy Icon/RewardId/Amount From Linked"))
            {
                CopyRewardsFromLinked((CardCollectionRewardsConfigSO[])targets, _linkedConfig);
            }

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
        
        private static void CopyRewardsFromLinked(
            CardCollectionRewardsConfigSO[] selectedConfigs,
            CardCollectionRewardsConfigSO linkedConfig)
        {
            if (selectedConfigs == null || selectedConfigs.Length == 0)
            {
                return;
            }
            
            if (linkedConfig == null)
            {
                Debug.LogWarning("[CardCollectionRewardsConfigSOEditor] Linked Config is not assigned.");
                return;
            }

            if (linkedConfig.GroupRewards == null || linkedConfig.GroupRewards.Length == 0)
            {
                Debug.LogWarning("[CardCollectionRewardsConfigSOEditor] Linked Config has no GroupRewards.");
                return;
            }
            
            var linkedByGroupId = new Dictionary<string, CollectionCompletionRewardConfig>();
            foreach (var linkedReward in linkedConfig.GroupRewards)
            {
                if (string.IsNullOrWhiteSpace(linkedReward.GroupId))
                {
                    continue;
                }
                
                linkedByGroupId[linkedReward.GroupId] = linkedReward;
            }

            foreach (var config in selectedConfigs)
            {
                if (config == null || config == linkedConfig || config.GroupRewards == null)
                {
                    continue;
                }
                
                Undo.RecordObject(config, "Copy Card Collection Group Rewards From Linked");
                var rewards = config.GroupRewards;
                for (var i = 0; i < rewards.Length; i++)
                {
                    var current = rewards[i];
                    if (string.IsNullOrWhiteSpace(current.GroupId))
                    {
                        continue;
                    }

                    if (!linkedByGroupId.TryGetValue(current.GroupId, out var linkedValue))
                    {
                        continue;
                    }
                    
                    current.Icon = linkedValue.Icon;
                    current.RewardId = linkedValue.RewardId;
                    current.Amount = linkedValue.Amount;
                    rewards[i] = current;
                }

                config.GroupRewards = rewards;
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
