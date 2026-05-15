using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TileEditor
{
    public partial class TileEditor
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void UpdateBrush(int x, int y, bool isCellHovered)
        {
            if (isCellHovered)
            {
                var gridCell = GetGridCell(x, y);
                _brushContainer.transform.position = gridCell.transform.position;
            }
        }

        public void AddObject(int x, int y)
        {
            var editorTileCell = GetGridCell(x, y);
            _currentBrushObject.PrepareToBrush(editorTileCell);
            var objectModel = _currentBrushObject.SaveObject();
            objectModel.cellX = x;
            objectModel.cellY = y;
            objectModel.instanceId = UIDGenerator.Get();
            ExecuteCommand(new AddObjectCommand(CurrentLocation, objectModel));
            _currentBrushObject.FinishBrush(editorTileCell);
        }

        public void SetBrushContainerPosition(Vector3 position)
        {
            _brushContainer.transform.position = position;
        }

        public Vector3 GetBrushContainerPosition() => _brushContainer.transform.position;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Brush logic

        private Transform _brushContainer;
        private LocationObject _currentBrushObject;

        public void SelectObjectAsBrush(LocationObject inspectedObject)
        {
            _addObjectButtons[inspectedObject.Uid].SelectTool();
            _currentBrushObject.Clone(inspectedObject);
            _brushInspectorPanel.InitBrush(_currentBrushObject);
        }

        public void ObjectToolSelectedHandler(LocationObject obj)
        {
            if (_currentBrushObject != null) DestroyBrush();

            if (_brushContainer == null) CreateBrushRoot();

            _currentBrushObject = LocationObjectsFactory.Create(new LocationObjectModel {objectId = obj.Uid}, _brushContainer).Result;
            _currentBrushObject.SetBrushMode();
            _brushInspectorPanel.InitBrush(_currentBrushObject);
        }

        public void ObjectToolDeselectedHandler(LocationObject obj)
        {
            if (_currentBrushObject != null && _currentBrushObject.Uid == obj.Uid) DestroyBrush();
        }

        private void DestroyBrush()
        {
            _brushInspectorPanel.InitBrush(null);

            if (_currentBrushObject == null) return;

            Destroy(_currentBrushObject.gameObject);
            _currentBrushObject = null;
        }

        private void CreateBrushRoot()
        {
            var go = new GameObject("Brush");
            go.transform.SetParent(_tileCellContainer);
            _brushContainer = go.transform;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Inspect object logic

        private List<LocationObject> _lastInspectedObjects;

        public void InspectCellObjects(int x, int y)
        {
            if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && _lastInspectedObjects != null && _lastInspectedObjects.Count > 0)
            {
                // add new items to selection
                var cell = CurrentLocation.GetCell(x, y);
                if (cell == null) return;

                List<LocationObject> inspectedObjects = cell.IterateObjects().ToList();
                if (inspectedObjects.Count == 0) return;

                inspectedObjects.ForEach(lo =>
                {
                    if (!_lastInspectedObjects.Contains(lo)) _lastInspectedObjects.Add(lo);
                });

                InspectObjectsList(_lastInspectedObjects);
            }
            else
            {
                var cell = CurrentLocation.GetCell(x, y);
                if (cell == null)
                {
                    InspectObjectsList(null);
                    return;
                }

                List<LocationObject> inspectedObjects = cell.IterateObjects().ToList();
                InspectObjectsList(inspectedObjects);
            }
        }

        public void InspectObject(LocationObject locationObject, bool moveCamera)
        {
            MoveCamera(locationObject.transform.position);
            InspectObjectsList(new List<LocationObject> { locationObject });
        }

        public void InspectObjectsList(List<LocationObject> inspectedObjects)
        {
            if (CurrentToolKind != ToolKind.InspectObjects) ToggleTool(ToolKind.InspectObjects);

            _lastInspectedObjects = inspectedObjects == null ? null : new List<LocationObject>(inspectedObjects);
            _objectsListInspector.Inspect(inspectedObjects);
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<string, ToolButton> _addObjectButtons = new Dictionary<string, ToolButton>();

        public void RegisterAddObjectButton(LocationObject obj, ToolButton objectTool)
        {
            _addObjectButtons[obj.Uid] = objectTool;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}