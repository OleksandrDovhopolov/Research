using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabros.TileEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Module.TileEditor
{
    public class GroupedLocationObjectGetter : MonoBehaviour, ILocationObjectsGetter
    {
        
        [SerializeField] private List<LocationGroup> _locationGroups;

        private static string ObjectsDirectoryPath => Path.Combine(Application.dataPath);

        public List<LocationObject> GetAllLocationObjects()
        {
            var locationObjects = new List<LocationObject>();

            foreach (var locationGroup in _locationGroups)
            {
                var groupObjects = locationGroup.LocationObjects;
                locationObjects.AddRange(groupObjects);
            }

            return locationObjects;
        }

#if UNITY_EDITOR

        public void AddAssetsToGroupByPath(string filePath, string groupName, string addressableGroupName = "")
        {
            var group = _locationGroups.Find(group => group.Name == groupName);
            if (group == null)
            {
                Debug.LogWarning($"Failed to find group with ID Camp");
                return;
            }

            var path = Path.Combine(ObjectsDirectoryPath, filePath);
            var subCategory = path.Split('/').Last();

            var addressableGroup = string.IsNullOrEmpty(addressableGroupName)
                ? group.AddressableGroupName
                : addressableGroupName;
            
            if (string.IsNullOrEmpty(addressableGroup))
            {
                Debug.LogWarning($"Failed to resolve addressableGroup for group {group.Name}");
                return;
            }
            
            RegisterAssetsFromDirectory(path, group.Name, subCategory,group.Name, addressableGroup);
        }
        
        //[Button]  require NaughtyAttributes;
        private void FillCampLocationObjects()
        {
            var campGroup = _locationGroups.Find(group => group.Name == "Camp");
            if (campGroup == null)
            {
                Debug.LogWarning($"Failed to find group with ID Camp");
                return;
            }

            campGroup.ClearAssets();
            FillLocationObjectsForGroup(campGroup, campGroup.PrefabFolder);
        }

        private void FillLocationObjectsForGroup(LocationGroup locationGroup, string prefabPath)
        {
            var groupName = locationGroup.Name;
            var addressableGroupName = locationGroup.AddressableGroupName;

            var path = Path.Combine(ObjectsDirectoryPath, prefabPath);
            
            foreach (var subCategoryDirectory in Directory.GetDirectories(path))
            {
                var subCategory = new DirectoryInfo(subCategoryDirectory).Name;
                RegisterAssetsFromDirectory(subCategoryDirectory, groupName, subCategory, groupName, addressableGroupName);
            }
        }

        private void RegisterAssetsFromDirectory(string directoryPath, string category, string subCategory,
            string groupName, string addressableGroupName)
        {
            foreach (string assetFile in Directory.GetFiles(directoryPath, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetExtension(assetFile) != ".prefab") continue;
                RegisterAsset(assetFile, category, subCategory, groupName, addressableGroupName);
            }
        }

        private void RegisterAsset(string assetFilePath, string category, string subCategory, string groupName, string addressableGroupName)
        {
            var relativeAssetPath = assetFilePath.Replace(Application.dataPath, "Assets");
            var asset = AssetDatabase.LoadAssetAtPath<LocationObject>(relativeAssetPath);

            if (asset == null)
                Debug.LogError($"[Tile Editor] Asset hasn't 'LocationObject' component: {relativeAssetPath}");

            var mansionGroup = _locationGroups.Find(group => group.Name == groupName);
            mansionGroup.AddAsset(asset);

            asset.Category = category;
            asset.SubCategory = subCategory;
            PrefabUtility.SavePrefabAsset(asset.gameObject);

            var fileRelativePath = assetFilePath.Substring(assetFilePath.IndexOf("Assets", StringComparison.Ordinal));
            AddAssetToAddressableGroup(fileRelativePath, $"{Path.GetFileNameWithoutExtension(assetFilePath)}.tile",
               addressableGroupName);
        }

        private void AddAssetToAddressableGroup(string filePath, string fileAddress, string addressableGroupName)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            var group = aaSettings.FindGroup(addressableGroupName);
            if (group == null)
            {
                Debug.LogWarning($"Addressable : can't find group {addressableGroupName}");
                return;
            }

            var entry = aaSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(filePath), group);
            if (entry == null)
            {
                Debug.LogWarning($"Addressable : can't add file {filePath} to group {addressableGroupName}");
                return;
            }

            entry.address = fileAddress;
            aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryAdded, entry, true);
        }
#endif
    }
    
    [Serializable]
    public class LocationGroup
    {
        [field: SerializeField] public string Name { private set; get; }
        [field: SerializeField] public string AddressableGroupName { private set; get; }
        [field: SerializeField] public string PrefabFolder { private set; get; }
        [field: SerializeField] public List<LocationObject> LocationObjects { private set; get; }

        public void ClearAssets()
        {
            LocationObjects.Clear();
        }

        public void AddAsset(LocationObject locationObject)
        {
            if (LocationObjects.Contains(locationObject)) return;
            
            LocationObjects.Add(locationObject);
        }
    }
}