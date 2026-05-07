using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fabros.TileEditor
{
    public class LocationCell
    {
        //--------------------------------------------------------------------------------------------------------------------------

        private readonly List<LocationObject> _cellObjects = new List<LocationObject>();

        public Vector2Int LogicPosition { get; private set; }
        public int X => LogicPosition.x;
        public int Y => LogicPosition.y;

        private TileEditorSettings _settings;

        public Location Location { get; private set; }

        public void InitCell(Location location, int x, int y, TileEditorSettings settings)
        {
            LogicPosition = new Vector2Int(x, y);
            Location = location;
            _settings = settings;
        }

        public bool AddObject(LocationObject locationObject, LocationObjectModel model, bool isEditor)
        {
            locationObject.Init(this, model, isEditor);

            if (AddObject(locationObject))
            {
                return true;
            }

            Object.Destroy(locationObject.gameObject);
            return false;
        }

        public bool AddObject(LocationObject locationObject)
        {
            if (!_cellObjects.Contains(locationObject) && CanAddObject(locationObject))
            {
                _cellObjects.Add(locationObject);
                locationObject.AssignCell(this);
                var tr = locationObject.transform;
                tr.SetParent(Location.transform, false);
                tr.localPosition = new Vector3(X * _settings.gridSizeX, 0, Y * _settings.gridSizeY);
                return true;
            }

            return false;
        }

        public bool CanAddObject(LocationObject locationObject)
        {
            if (Location.AllowAddObjectToSameCell) return true;

            foreach (LocationObject existsObject in _cellObjects.Where(obj => obj.Uid == locationObject.Uid))
            {
                if (existsObject.AllowAddObjectWithSameId(locationObject, out string denyReason)) continue;

                Debug.LogWarning($"[ LocationCell ({X},{Y}) ]: can't add object. Reason: {denyReason}");
                return false;
            }

            return true;
        }

        public void RemoveObject(LocationObject locationObject, bool autoDestroy = true)
        {
            if (_cellObjects.Remove(locationObject) && autoDestroy) Object.Destroy(locationObject.gameObject);
        }

        public IEnumerable<LocationObject> IterateObjects() => _cellObjects;

        public int GetObjectsCount() => _cellObjects.Count;

        public LocationCell GetCellInDirection(Vector2Int direction)
        {
            if (direction == Vector2Int.right) return Location.GetOrAddCell(X + 1, Y);
            if (direction == Vector2Int.left) return Location.GetOrAddCell(X - 1, Y);
            if (direction == Vector2Int.up) return Location.GetOrAddCell(X, Y + 1);
            if (direction == Vector2Int.down) return Location.GetOrAddCell(X, Y - 1);

            Debug.LogWarning($"Warning! Bad direction {direction}. Default directions expected.");
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public void SaveObjectsToList(List<LocationObjectModel> objectsList)
        {
            foreach (var locationObject in _cellObjects) objectsList.Add(locationObject.SaveObject());
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}