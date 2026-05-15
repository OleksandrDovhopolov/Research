using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TileEditor
{
    public class CampLocationObjectGetter : MonoBehaviour, ILocationObjectsGetter
    {
        private const string CampCategoryName = "Camp";
        private const string CampPrefabRootPath = "Assets/Location/Environment/Prefab/Camp";

        public List<LocationObject> GetAllLocationObjects()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder(CampPrefabRootPath))
            {
                Debug.LogWarning($"[Tile Editor] Can't find prefab root folder: {CampPrefabRootPath}");
                return new List<LocationObject>();
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { CampPrefabRootPath });
            var locationObjects = new List<LocationObject>(prefabGuids.Length);

            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var locationObject = prefab != null ? prefab.GetComponent<LocationObject>() : null;
                if (locationObject == null)
                {
                    Debug.LogWarning($"[Tile Editor] Prefab has no LocationObject component: {prefabPath}");
                    continue;
                }

                locationObject.Category = CampCategoryName;
                locationObject.SubCategory = ResolveSubCategory(prefabPath);
                locationObjects.Add(locationObject);
            }

            return locationObjects
                .OrderBy(locationObject => locationObject.SubCategory)
                .ThenBy(locationObject => locationObject.Name)
                .ToList();
#else
            return new List<LocationObject>();
#endif
        }

#if UNITY_EDITOR
        private static string ResolveSubCategory(string prefabPath)
        {
            var normalizedPath = prefabPath.Replace("\\", "/");
            var directoryPath = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return string.Empty;
            }

            if (directoryPath.Equals(CampPrefabRootPath))
            {
                return string.Empty;
            }

            var rootWithSlash = $"{CampPrefabRootPath}/";
            return directoryPath.StartsWith(rootWithSlash)
                ? directoryPath.Substring(rootWithSlash.Length)
                : string.Empty;
        }
#endif
    }
}
