using System.Collections;
using UnityEngine;

namespace TileEditor
{
    public class Orthographic2DCameraController : BaseCameraController
    {
        //--------------------------------------------------------------------------------------------------------------------------

        private const float _X_MULTIPLIER = .6f;
        private const float _Z_MULTIPLIER = 1.4f;
        private const float _CAMERA_MOVE_SPEED = 6f;
        private const float _ZOOM_MIN = 1;
        private const float _ZOOM_MAX = 25;
        private const float _ZOOM_STEP = 0.3f;

        //--------------------------------------------------------------------------------------------------------------------------

        private Vector3 _lastMousePosition;
        private Vector3 _cameraStartPosition;
        private Coroutine _moveCameraRoutine;

        //--------------------------------------------------------------------------------------------------------------------------

        protected override void DoInit()
        {
            _camera.orthographic = true;
            _cameraStartPosition = _cameraTransform.position;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public override void SetDefaultPosition()
        {
            _cameraTransform.position = _cameraStartPosition;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private Vector3 GetMousePosition()
        {
            return _camera.transform.InverseTransformPoint(_camera.ScreenToWorldPoint(Input.mousePosition));
        }

        public override void StartCameraDrag()
        {
            base.StartCameraDrag();

            _lastMousePosition = GetMousePosition();
        }

        protected override void DoCameraDrag()
        {
            Vector3 mousePosition = GetMousePosition();
            Vector3 delta = _lastMousePosition - mousePosition;

            delta.z += delta.y * _Z_MULTIPLIER;
            delta.x += delta.y * _X_MULTIPLIER;
            delta.y = 0;

            _cameraTransform.position += delta;
            _lastMousePosition = mousePosition;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        protected override void DoCameraMove(Vector3 targetPosition)
        {
            targetPosition.y = 0;

            _moveCameraRoutine = StartCoroutine(MoveCameraRoutine(targetPosition));
        }

        private IEnumerator MoveCameraRoutine(Vector3 position)
        {
            while (Vector3.Distance(_cameraTransform.position, position) > .01f)
            {
                _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, position, _CAMERA_MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            _cameraTransform.position = position;
        }

        protected override void StopCameraMove()
        {
            if (_moveCameraRoutine != null)
            {
                StopCoroutine(_moveCameraRoutine);
                _moveCameraRoutine = null;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        
        public override void ZoomOut()
        {
            var newSize = Mathf.Max(_camera.orthographicSize - _ZOOM_STEP, _ZOOM_MIN);
            _camera.orthographicSize = newSize;
        }

        public override void ZoomIn()
        {
            var newSize = Mathf.Min(_camera.orthographicSize + _ZOOM_STEP, _ZOOM_MAX);
            _camera.orthographicSize = newSize;
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}

