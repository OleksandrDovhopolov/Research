using core;
using UnityEditor;
using UnityEngine;

namespace EventOrchestration.Core
{
    [CustomEditor(typeof(ScheduleTimeGeneratorPanel))]
    public sealed class ScheduleTimeGeneratorPanelEditor : Editor
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
