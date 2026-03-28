using UnityEditor;
using UnityEngine;

namespace core
{
    [CustomEditor(typeof(ScheduleTimeGeneratorPanel))]
    public sealed class ScheduleTimeGeneratorPanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8f);
            if (GUILayout.Button("Generate Schedule (+2 min / 10 min)"))
            {
                var panel = (ScheduleTimeGeneratorPanel)target;
                panel.GenerateSingleCardCollectionSchedule();
            }
        }
    }
}
