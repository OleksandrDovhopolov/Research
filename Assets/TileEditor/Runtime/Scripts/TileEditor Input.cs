using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TileEditor
{
    public partial class TileEditor
    {
        //--------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Camera _camera;

        private Action<int, int> _customClickAction;

        //--------------------------------------------------------------------------------------------------------------------------

        private Coroutine _moveCameraRoutine;
        private BaseCameraController _cameraController;

        private void ResetCameraPosition() => _cameraTransform.position = Vector3.zero;

        public void SetCustomClickAction(Action<int, int> action)
        {
            _customClickAction = action;
        }

        public void SetCurrentLocationPanelActive(bool isActive)
        {
            _activeLocationPanel.gameObject.SetActive(isActive);
        }

        public void SetInspectorPanelActive(bool isActive)
        {
            _objectsListInspector.gameObject.SetActive(isActive);
        }

        private void UpdateInput()
        {
            if (CurrentLocation == null) return;

            // drag
            if (Input.GetMouseButtonDown(1)) _cameraController.StartCameraDrag();
            if (Input.GetMouseButtonUp(1)) _cameraController.StopCameraDrag();
            _cameraController.UpdateCameraDrag();

            // zoom
            var mouseScrollDelta = Input.mouseScrollDelta.y;
            if (mouseScrollDelta > 0) _cameraController.ZoomOut();
            else if (mouseScrollDelta < 0) _cameraController.ZoomIn();

            if (_customClickAction != null || IsAnyPopupOpened || EventSystem.current.currentSelectedGameObject != null) return;

            // hotkeys
            if (Input.GetKeyDown(KeyCode.L)) ToggleTool(ToolKind.InspectLocation);
            if (Input.GetKeyDown(KeyCode.G)) ToggleTool(ToolKind.InspectGroups);
            if (Input.GetKeyDown(KeyCode.O)) ToggleTool(ToolKind.AddObject);
            if (Input.GetKeyDown(KeyCode.I)) ToggleTool(ToolKind.InspectObjects);
            if (Input.GetKeyDown(KeyCode.S)) ToggleTool(ToolKind.InspectLayers);
            if (Input.GetKeyDown(KeyCode.C)) _simpleButtons.Find(btn => btn.ButtonText == ActiveLocationPanel.TOGGLE_GRID_CELL_COORDS_BUTTON_NAME).ExecuteAction();
            if (Input.GetKeyDown(KeyCode.R)) _simpleButtons.Find(btn => btn.ButtonText == ActiveLocationPanel.TOGGLE_GRID_CELL_RENDERER_BUTTON).ExecuteAction();

            if (Input.GetKeyDown(KeyCode.Escape)) DeselectCurrentTool();

            if (Input.GetKeyDown(KeyCode.Y)) TryRedo();
            if (Input.GetKeyDown(KeyCode.Z)) TryUndo();
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Move to target

        public void MoveCamera(Vector3 position)
        {
            _cameraController.MoveCamera(position);
        }
        
        public Vector3 GetCameraPosition() => _cameraTransform.position;

        //--------------------------------------------------------------------------------------------------------------------------
    }
}