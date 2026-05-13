using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Module.TileEditor
{
    [CreateAssetMenu(fileName = "TileEditorLocationContainer", menuName = "TileEditor/Create LocationContainer")]
    public class EditorLocationContainer : ScriptableObject
    {
        [SerializeField] private List<EditorLocationInfo> _editorLocationInfos;
        private const string DefaultLocationAssetPath = "Assets/Game/Content/Locations";
        private string _objectsDirectoryPath => Path.Combine(Application.dataPath, "Location");
        private string _defaultLocationDirectoryPath => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Game", "Content", "Locations");
        
        public string GenerateFullPathToPrefab(string folderName)
        {
            return Path.Combine(_objectsDirectoryPath,folderName);
        }
        
        public List<EditorLocationInfo> GetLocationsInfo()
        {
            return _editorLocationInfos;
        }
        
        public EditorLocationInfo GetLocationInfo(string locationName)
        {
            return _editorLocationInfos.FirstOrDefault(a=>a.LocationName == locationName);
        }

        public string GenerateFullPathToLocation(string locationName)
        {
#if UNITY_EDITOR
            var location = _editorLocationInfos.FirstOrDefault(a=>a.LocationName == locationName);
            return Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(location.Location));
#else
            return string.Empty;
#endif
        }

        public void CreateNewEditorInfo(string locationName)
        {
#if UNITY_EDITOR
            Directory.CreateDirectory(_defaultLocationDirectoryPath);

            var newLocationInfo = new EditorLocationInfo(locationName);
            var path = EditorUtility.SaveFilePanel("Create zone", _defaultLocationDirectoryPath, locationName, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var projectRootPath = Directory.GetCurrentDirectory().Replace("\\", "/");
            var normalizedPath = path.Replace("\\", "/");
            if (!normalizedPath.StartsWith(projectRootPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[Tile Editor] Location json must be created inside the project. Path: {path}");
                return;
            }

            var relativePath = normalizedPath.Substring(projectRootPath.Length + 1);
            if (!relativePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning($"[Tile Editor] Failed to convert path to project-relative asset path. Path: {path}");
                return;
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }

            AssetDatabase.ImportAsset(relativePath);
            newLocationInfo.SetNewLocationAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath));
            if (newLocationInfo.Location == null)
            {
                Debug.LogWarning($"[Tile Editor] Failed to import location json as TextAsset: {relativePath}");
                return;
            }

            _editorLocationInfos ??= new List<EditorLocationInfo>();
            _editorLocationInfos.Add(newLocationInfo);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

        public bool IsExist(string locationName)
        {
            return _editorLocationInfos.Exists((a) => a.LocationName == locationName);
        }
    }
    
    [Serializable]
    public class EditorLocationInfo
    {
        public string LocationName => Location != null ? Location.name : null;
        [field:SerializeField] public TextAsset Location {private set; get;}
        [field:SerializeField] public string GroupName {private set; get;}
        [field:SerializeField] public List<string> PrefabFolders {private set; get;}

        public EditorLocationInfo(string locationName)
        {
            GroupName = locationName;
            PrefabFolders = new List<string> { locationName };
        }

        public void SetNewLocationAsset(TextAsset location)
        {
            Location = location;
        }
    }
}
