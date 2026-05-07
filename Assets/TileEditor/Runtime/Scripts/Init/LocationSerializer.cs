using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabros.TileEditor;
using UnityEditor;
using VContainer;

namespace Module.TileEditor
{
    public class LocationSerializer : ILocationsSerializer
    {
        private EditorLocationContainer _editorLocationContainer;
        
        [Inject]
        public void Install(EditorLocationContainer editorLocationContainer)
        {
            _editorLocationContainer = editorLocationContainer;
        }

        public string LoadLocation(string locationName)
        {
            return File.ReadAllText(_editorLocationContainer.GenerateFullPathToLocation(locationName));
        }

        public List<string> GetAllLocations()
        {
            return _editorLocationContainer.GetLocationsInfo().Select(a => a.LocationName).ToList();
        }

        public void SaveLocation(string locationName, string locationData)
        {
            if (!_editorLocationContainer.IsExist(locationName))
            {
                _editorLocationContainer.CreateNewEditorInfo(locationName);
            }
            File.WriteAllText(_editorLocationContainer.GenerateFullPathToLocation(locationName), locationData);
#if UNITY_EDITOR
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_editorLocationContainer.GetLocationInfo(locationName).Location));
#endif
        }

        public void DeleteLocation(string locationName)
        {
            File.Delete(_editorLocationContainer.GenerateFullPathToLocation(locationName));
        }
    }
}