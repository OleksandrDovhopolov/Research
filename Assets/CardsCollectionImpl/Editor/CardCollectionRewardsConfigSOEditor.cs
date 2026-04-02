using CardCollection.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CardCollectionImpl.Editor
{
    [CustomEditor(typeof(CardEventRewardsConfigSO))]
    public class CardCollectionRewardsConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Group Rewards"))
            {
                ClearRewards((CardEventRewardsConfigSO)target);
            }

            if (GUILayout.Button("Sync Group Rewards From GroupsJson"))
            {
                SyncRewards((CardEventRewardsConfigSO)target);
            }

            if (GUILayout.Button("Export Rewards Json TextAsset"))
            {
                ExportRewardsJson((CardEventRewardsConfigSO)target);
            }
        }

        private static void ClearRewards(CardEventRewardsConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            Undo.RecordObject(config, "Clear Card Collection Group Rewards");
            config.EventRewardsList = new CollectionCompletionRewardConfig[0];
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SyncRewards(CardEventRewardsConfigSO config)
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
            if (config.EventRewardsList != null)
            {
                foreach (var existing in config.EventRewardsList)
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
                    RewardId = string.Empty
                };
            }

            Undo.RecordObject(config, "Sync Card Collection Group Rewards");
            config.EventRewardsList = rewards;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ExportRewardsJson(CardEventRewardsConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            // Generate JSON via CardEventRewardsConfigSO.ToString()
            var json = config.ToString();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[CardCollectionRewardsConfigSOEditor] Generated JSON is empty.");
                return;
            }

            // Find the folder where CardEventRewardsConfigSO.cs script is located
            var scriptGuids = AssetDatabase.FindAssets("CardEventRewardsConfigSO t:Script");
            string scriptsFolder = "Assets/CardsCollectionImpl/Scripts/Rewards/ScriptableObjects";
            if (scriptGuids != null && scriptGuids.Length > 0)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    scriptsFolder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/')
                        ?? scriptsFolder;
                }
            }

            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(scriptsFolder))
            {
                Debug.LogError($"[CardCollectionRewardsConfigSOEditor] Target folder does not exist: {scriptsFolder}");
                return;
            }

            // Build file path and write contents
            var fileName = "CardsEventRewardsConfig.json";
            var assetPath = $"{scriptsFolder}/{fileName}";
            var fullPath = Path.GetFullPath(assetPath);

            try
            {
                File.WriteAllText(fullPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardCollectionRewardsConfigSOEditor] Failed to write JSON file: {e}");
                return;
            }

            // Import or reimport, then assign to config.GroupsJson
            AssetDatabase.ImportAsset(assetPath);
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (textAsset == null)
            {
                Debug.LogError("[CardCollectionRewardsConfigSOEditor] Failed to load created TextAsset.");
                return;
            }

            Undo.RecordObject(config, "Export Rewards Json TextAsset");
            config.GroupsJson = textAsset;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardCollectionRewardsConfigSOEditor] Exported rewards JSON to: {assetPath}");
        }
    }
}
