using System;
using TileEditor;
using UnityEngine;

namespace Game.Features.Locations
{
    [Obsolete]
    public sealed class RuntimeOrthographicLocationCameraController : MonoBehaviour
    {
        private const float ZoomMin = 1f;
        private const float ZoomMax = 25f;
        private const float ZoomStep = 1f;

        [SerializeField] private Camera _camera;
        [SerializeField] private TileEditorSettings _settings;
        [SerializeField] private int _dragMouseButton = 1;
        [SerializeField] private bool _enableMouseWheelZoom = true;

        private bool _isDragging;
        private Vector3 _lastMouseWorldPosition;

        public Vector3 CameraPosition => _camera != null ? _camera.transform.position : Vector3.zero;

        private void Awake()
        {
            ResolveCamera();
            ConfigureCamera();
        }

        public void Init(Camera targetCamera, TileEditorSettings settings)
        {
            if (targetCamera != null)
            {
                _camera = targetCamera;
            }

            if (settings != null)
            {
                _settings = settings;
            }

            ConfigureCamera();
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
            if (Input.GetMouseButtonDown(_dragMouseButton))
            {
                _isDragging = TryGetMouseWorldPosition(out _lastMouseWorldPosition);
                return;
            }

            if (Input.GetMouseButtonUp(_dragMouseButton))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging || !Input.GetMouseButton(_dragMouseButton))
            {
                return;
            }

            if (!TryGetMouseWorldPosition(out var mouseWorldPosition))
            {
                return;
            }

            var delta = _lastMouseWorldPosition - mouseWorldPosition;
            var position = _camera.transform.position;
            position.x += delta.x;
            position.z += delta.z;
            _camera.transform.position = position;

            _lastMouseWorldPosition = mouseWorldPosition;
        }

        private void UpdateZoom()
        {
            if (!_enableMouseWheelZoom)
            {
                return;
            }

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

        private void ResolveCamera()
        {
            if (_camera != null)
            {
                return;
            }

            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void ConfigureCamera()
        {
            if (_camera != null)
            {
                _camera.orthographic = true;
            }
        }
    }
}
