using UnityEditor;
using UnityEngine;

namespace core
{
    [CustomEditor(typeof(CardSettingsScriptableObject))]
    public class CardSettingsSOEditor : Editor
    {
        private Sprite spriteForAll;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var so = (CardSettingsScriptableObject)target;

            GUILayout.Space(15);
            GUILayout.Label("Tools", EditorStyles.boldLabel);

            // -----------------------
            // GENERATE 150 ENTRIES
            // -----------------------
            if (GUILayout.Button("Generate 150 Card Entries (ID 1 → 150)"))
            {
                Undo.RecordObject(so, "Generate Card Entries");

                so.CardSprites.Clear();

                for (int i = 1; i <= 150; i++)
                {
                    so.CardSprites.Add(new CardSpriteSetting
                    {
                        CardId = i,
                        //Address = "" // пусто пока нет адресов
                    });
                }

                EditorUtility.SetDirty(so);
                Debug.Log("Generated 150 card sprite entries!");
            }

            GUILayout.Space(10);

            // -----------------------
            // APPLY ONE SPRITE TO ALL
            // -----------------------
            spriteForAll = EditorGUILayout.ObjectField(
                "Sprite for All Cards",
                spriteForAll,
                typeof(Sprite),
                false
            ) as Sprite;

            if (spriteForAll != null)
            {
                if (GUILayout.Button("Apply Sprite to All Cards"))
                {
                    Undo.RecordObject(so, "Assign Sprite To All");

                    foreach (var item in so.CardSprites)
                    {
                        item.Sprite = spriteForAll;
                    }

                    EditorUtility.SetDirty(so);
                    Debug.Log($"Assigned sprite to all {so.CardSprites.Count} cards!");
                }
            }
        }
    }
}