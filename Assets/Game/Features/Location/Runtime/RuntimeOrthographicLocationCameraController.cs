using Fabros.TileEditor;
using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class RuntimeOrthographicLocationCameraController : MonoBehaviour
    {
        private const float ZoomMin = 1f;
        private const float ZoomMax = 25f;
        private const float ZoomStep = 1f;

        private Camera _camera;
        private TileEditorSettings _settings;
        private bool _isDragging;
        private Vector3 _lastMouseWorldPosition;

        public Vector3 CameraPosition => _camera != null ? _camera.transform.position : Vector3.zero;

        public void Init(Camera targetCamera, TileEditorSettings settings)
        {
            _camera = targetCamera;
            _settings = settings;

            if (_camera != null)
            {
                _camera.orthographic = true;
            }
        }

        private void Update()
        {
            if (_camera == null || _settings == null)
            {
                return;
            }

            UpdateDrag();
            UpdateZoom();
        }

        public Vector3Int WorldToTile(Vector3 worldPosition)
        {
            var gridSizeX = Mathf.Approximately(_settings.gridSizeX, 0f) ? 1f : _settings.gridSizeX;
            var gridSizeY = Mathf.Approximately(_settings.gridSizeY, 0f) ? 1f : _settings.gridSizeY;

            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x / gridSizeX),
                Mathf.RoundToInt(worldPosition.z / gridSizeY),
                0);
        }

        public void ZoomIn()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Max(ZoomMin, _camera.orthographicSize - ZoomStep);
        }

        public void ZoomOut()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.orthographicSize = Mathf.Min(ZoomMax, _camera.orthographicSize + ZoomStep);
        }

        private void UpdateDrag()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _isDragging = TryGetMouseWorldPosition(out _lastMouseWorldPosition);
                return;
            }

            if (Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging || !Input.GetMouseButton(1))
            {
                return;
            }

            if (!TryGetMouseWorldPosition(out var mouseWorldPosition))
            {
                return;
            }

            var delta = _lastMouseWorldPosition - mouseWorldPosition;
            _camera.transform.position += delta;
            _lastMouseWorldPosition = mouseWorldPosition;
        }

        private void UpdateZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            if (scroll > 0f)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }

        private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
        {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out var enter))
            {
                worldPosition = ray.GetPoint(enter);
                return true;
            }

            worldPosition = default;
            return false;
        }
    }
}
