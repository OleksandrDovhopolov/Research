using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.Editor
{
    public static class CheatsMenuItems
    {
        private const string EventCardsFolderName = "event_cards";
        private const string GlobalSaveFileName = "global_save.json";

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

        [MenuItem("Cheats/Clear Global Save CardCollections", priority = 2)]
        private static void ClearGlobalSaveCardCollections()
        {
            var savePath = Path.Combine(Application.persistentDataPath, GlobalSaveFileName);
            var confirmed = EditorUtility.DisplayDialog(
                "Clear Global Save CardCollections",
                $"Set CardCollections to empty array in:\n{savePath}",
                "Clear",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                if (!File.Exists(savePath))
                {
                    Debug.Log($"[CheatsMenuItems] Global save not found: {savePath}");
                    return;
                }

                var json = File.ReadAllText(savePath);
                var root = JObject.Parse(json);
                root["CardCollections"] = new JArray();
                File.WriteAllText(savePath, root.ToString(Formatting.Indented));

                Debug.Log($"[CheatsMenuItems] Cleared CardCollections in {savePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheatsMenuItems] Failed to clear CardCollections in global save: {ex}");
            }
        }
    }
}
