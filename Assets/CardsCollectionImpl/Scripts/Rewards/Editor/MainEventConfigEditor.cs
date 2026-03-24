using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl.Editor
{
    [CustomEditor(typeof(MainEventConfig))]
    public sealed class MainEventConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate All"))
            {
                var config = (MainEventConfig)target;
                config.ValidateAll();
                EditorUtility.SetDirty(config);
            }
        }
    }
}
