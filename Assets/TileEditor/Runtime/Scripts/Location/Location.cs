using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fabros.TileEditor
{
    public class Location : MonoBehaviour
    {
        private const int ImmediatelyInstantiatesRange = 20;
        private bool _isEditor;
        
        //--------------------------------------------------------------------------------------------------------------------------

        public string Name { get; private set; }

        public bool AllowAddObjectToSameCell => _settings.allowAddObjectToSameCell;

        private List<string> _locationGroups;
        private readonly Dictionary<Vector2Int, LocationCell> _locationCells = new Dictionary<Vector2Int, LocationCell>();
        private ILocationObjectsFactory _objectsFactory;
        private TileEditorSettings _settings;
        
        //--------------------------------------------------------------------------------------------------------------------------
        // Fabric methods

        public static async Task<Location> Create(Transform container, LocationModel locationModel, ILocationObjectsFactory objectsFactory, TileEditorSettings settings, bool isEditor)
        {
            var locationRoot = new GameObject($"Location {locationModel.name}");
            locationRoot.transform.SetParent(container, false);
            var location = locationRoot.AddComponent<Location>();
            location.Name = locationModel.name;
            location._objectsFactory = objectsFactory;
            location._settings = settings;
            location._locationGroups = locationModel.groups ?? new List<string>(); 
            location._isEditor = isEditor;
            await Task.WhenAll(locationModel.objects.Select(model=>location.AddObject(model)));
            return location;
        }
        
        public static async Task<(Location, List<LocationObjectModel>)> Create(Transform container,
            LocationModel locationModel,
            ILocationObjectsFactory objectsFactory,
            TileEditorSettings settings,
            Vector3Int startPoint, 
            Action<LocationObject> onLocationObjectCreated)
        {
            var locationRoot = new GameObject($"Location {locationModel.name}");
            locationRoot.transform.SetParent(container, false);
            var location = locationRoot.AddComponent<Location>();
            location.Name = locationModel.name;
            location._objectsFactory = objectsFactory;
            location._settings = settings;
            location._locationGroups = locationModel.groups ?? new List<string>();

            var immediatelyList = new List<LocationObjectModel>();
            var backgroundList = new List<LocationObjectModel>();
            var locationObjects = locationModel.objects.OrderBy(x => (new Vector3Int(x.cellX, x.cellY) - startPoint).magnitude);

            foreach (var locationObjectModel in locationObjects)
            {
                var objectPos = new Vector3Int(locationObjectModel.cellX, locationObjectModel.cellY);

                if ((objectPos - startPoint).magnitude <= ImmediatelyInstantiatesRange || locationObjectModel.needPreload)
                {
                    immediatelyList.Add(locationObjectModel);
                }
                else
                {
                    backgroundList.Add(locationObjectModel);
                }
            }
            
            await Task.WhenAll(immediatelyList.Select(model => location.AddObject(model, null, onLocationObjectCreated)));
            
            return (location, backgroundList);
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Public methods

        public async Task<LocationObject> AddObject(LocationObjectModel objectModel, CancellationTokenSource cancellationTokenSource = null, Action<LocationObject> onLocationObjectCreated = null)
        {
            var cell = GetOrAddCell(objectModel.cellX, objectModel.cellY);
            if (this == null) return null;
            var objectCreator = await _objectsFactory.Create(objectModel, transform, cancellationTokenSource);
            
            if (objectCreator == null) return null;
            if (cell.AddObject(objectCreator, objectModel, _isEditor))
            {
                onLocationObjectCreated?.Invoke(objectCreator);
                return objectCreator;
            }
            
            return null;
        }

        public LocationCell GetCell(int x, int y)
        {
            return _locationCells.GetValueOrDefault(new Vector2Int(x, y));
        }

        public LocationCell GetOrAddCell(int x, int y)
        {
            return GetCell(x, y) ?? AddCell(x, y);
        }

        private LocationCell AddCell(int x, int y)
        {
            var locationCell = new LocationCell();
            locationCell.InitCell(this, x, y, _settings);
            _locationCells.Add(new Vector2Int(x, y), locationCell);
            return locationCell;
        }

        public IEnumerable<LocationObject> IterateObjects() =>
            _locationCells.Values.SelectMany(cell => cell.IterateObjects());

        //--------------------------------------------------------------------------------------------------------------------------
        // Groups

        public bool IsGroupExists(string groupName) => _locationGroups.Exists(group => group == groupName);

        public void AddGroup(string groupName)
        {
            if (IsGroupExists(groupName)) return;

            _locationGroups.Add(groupName);
        }

        public void RemoveGroup(string groupName)
        {
            _locationGroups.Remove(groupName);

            foreach (var locationObject in GetObjectsFromGroup(groupName)) 
                locationObject.RemoveFromGroup(groupName);
        }

        public IEnumerable<string> IterateGroups() => _locationGroups;

        public IEnumerable<LocationObject> GetObjectsFromGroup(string groupName) =>
            IterateObjects().Where(obj => obj.IsInGroup(groupName));

        public LocationObject GetObjectByInstanceId(string instanceId)
        {
            foreach (var locationCell in _locationCells)
            {
                var obj = locationCell.Value.IterateObjects().FirstOrDefault(lo => lo.InstanceId == instanceId);
                if (obj != null) return obj;
            }

            return null;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public LocationModel SaveLocation()
        {
            var result = new LocationModel {name = Name};

            var locationObjectModels = new List<LocationObjectModel>();

            foreach (var cell in _locationCells.Values)
            {
                cell.SaveObjectsToList(locationObjectModels);
            }
            
            result.objects = locationObjectModels;
            result.groups = _locationGroups;

            return result;
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}