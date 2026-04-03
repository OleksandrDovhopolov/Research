using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.Editor
{
    public static class CheatsMenuItems
    {
        private const string EventCardsFolderName = "event_cards";

        [MenuItem("Cheats/Clear Event Cards Storage", priority = 1)]
        private static void ClearEventCardsStorage()
        {
            var rootPath = Path.Combine(Application.persistentDataPath, EventCardsFolderName);

            var confirmed = EditorUtility.DisplayDialog(
                "Clear Event Cards Storage",
                $"Delete all json files in:\n{rootPath}",
                "Clear",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                if (!Directory.Exists(rootPath))
                {
                    Debug.Log($"[CheatsMenuItems] Folder not found: {rootPath}");
                    return;
                }

                var files = Directory.GetFiles(rootPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                Debug.Log($"[CheatsMenuItems] Cleared {files.Length} file(s) in {rootPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheatsMenuItems] Failed to clear event cards storage: {ex}");
            }
        }
    }
}
