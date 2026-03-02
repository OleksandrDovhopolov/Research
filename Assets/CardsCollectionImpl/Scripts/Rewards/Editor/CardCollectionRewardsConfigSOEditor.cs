using CardCollection.Core;
using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl.Editor
{
    [CustomEditor(typeof(CardCollectionRewardsConfigSO))]
    public class CardCollectionRewardsConfigSOEditor : UnityEditor.Editor
    {
        private static readonly string[] RewardIds = { "Gems", "Gold", "Energy" };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate 15 Group Rewards"))
            {
                GenerateRewards((CardCollectionRewardsConfigSO)target);
            }
        }

        private static void GenerateRewards(CardCollectionRewardsConfigSO config)
        {
            if (config == null)
            {
                return;
            }

            Undo.RecordObject(config, "Generate Card Collection Group Rewards");

            var rewards = new CollectionCompletionRewardConfig[15];
            for (var i = 0; i < rewards.Length; i++)
            {
                rewards[i] = new CollectionCompletionRewardConfig
                {
                    GroupId = (i + 1).ToString(),
                    RewardId = RewardIds[i % RewardIds.Length],
                    Amount = Random.Range(0, 101),
                    Icon = null
                };
            }

            config.GroupRewards = rewards;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
