using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class BootstrapPlayMode
{
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    static BootstrapPlayMode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (EditorSceneManager.GetActiveScene().path != BootstrapScenePath)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(BootstrapScenePath);
            }
        }
    }
}

