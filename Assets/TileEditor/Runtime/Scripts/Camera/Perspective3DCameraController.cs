using System.Collections;
using UnityEngine;

namespace TileEditor
{
    public class Perspective3DCameraController : BaseCameraController
    {
        //--------------------------------------------------------------------------------------------------------------------------

        private const float _CAMERA_MOVE_SPEED = 15f;
        private const float _CAMERA_ROTATION_SPEED = 2f;
        private const float _CAMERA_TARGET_DISTANCE = 10f;
        private const float _CAMERA_ACCELERATION_RATE = 2f;

        //--------------------------------------------------------------------------------------------------------------------------

        private readonly Vector3 _cameraStartPosition = new Vector3(0, 13, -13);
        private readonly Vector3 _cameraStartRotation = new Vector3(45, 0, 0);

        private Coroutine _moveCameraRoutine;
        private CameraAxis _axis;

        //--------------------------------------------------------------------------------------------------------------------------

        protected override void DoInit()
        {
            _camera.orthographic = false;

            _cameraTransform.localRotation = Quaternion.identity;

            SetDefaultPosition();

            _axis = Instantiate(Resources.Load<CameraAxis>("TileEditor_3D_Axis"));
            _axis.RotateAxis(_camera.transform.localEulerAngles);
        }

        public override void SetDefaultPosition()
        {
            _camera.transform.localPosition = _cameraStartPosition;
            _camera.transform.localRotation = Quaternion.Euler(_cameraStartRotation);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        protected override void DoCameraDrag()
        {
            //move
            var delta = Vector3.zero;
            var dragSpeed = _CAMERA_MOVE_SPEED * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftShift)) dragSpeed *= _CAMERA_ACCELERATION_RATE;
            if (Input.GetKey(KeyCode.W)) delta += _camera.transform.forward * dragSpeed;
            if (Input.GetKey(KeyCode.S)) delta += -1 * _camera.transform.forward * dragSpeed;
            if (Input.GetKey(KeyCode.D)) delta += _camera.transform.right * dragSpeed;
            if (Input.GetKey(KeyCode.A)) delta += -1 * _camera.transform.right * dragSpeed;
            if (Input.GetKey(KeyCode.E)) delta += _camera.transform.up * dragSpeed;
            if (Input.GetKey(KeyCode.Q)) delta += -1 * _camera.transform.up * dragSpeed;

            _camera.transform.localPosition += delta;

            //rotation
            var newRotationX = _camera.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * _CAMERA_ROTATION_SPEED;
            var newRotationY = _camera.transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * _CAMERA_ROTATION_SPEED;
            _camera.transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            _axis.RotateAxis(_camera.transform.localEulerAngles);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        protected override void DoCameraMove(Vector3 targetPosition)
        {
            _moveCameraRoutine = StartCoroutine(MoveCameraRoutine(targetPosition));
        }

        private IEnumerator MoveCameraRoutine(Vector3 targetPosition)
        {
            var position = targetPosition - _camera.transform.forward * _CAMERA_TARGET_DISTANCE - _camera.transform.right;

            while (Vector3.Distance(_camera.transform.position, position) > .01f)
            {
                _camera.transform.position = Vector3.Lerp(_camera.transform.position, position, _CAMERA_MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            _camera.transform.position = position;
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
            _camera.transform.localPosition += -1 * _camera.transform.forward * (_CAMERA_MOVE_SPEED * 2 * Time.deltaTime);
        }

        public override void ZoomIn()
        {
            _camera.transform.localPosition += _camera.transform.forward * (_CAMERA_MOVE_SPEED * 2 * Time.deltaTime);
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}

