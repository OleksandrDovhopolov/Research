using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;

namespace CameraModule
{
    public class CameraDragState : BaseCameraState
    {
        private const float TargetFrameDuration = 1f / 60f;

        public CameraDragState(CameraBehaviour cameraBehaviour, CameraSettings cameraSettings) : base(cameraBehaviour, cameraSettings)
        {
        }

        public override void LateUpdate()
        {
            var origin = _fingerFilter.IgnoreStartedOverGui;
            _fingerFilter.IgnoreStartedOverGui = false;
            var fingers = _fingerFilter.UpdateAndGetFingers();
            DragCamera(fingers);
            _fingerFilter.IgnoreStartedOverGui = origin;
        }
        
        private void DragCamera(List<LeanFinger> fingers)
        {
            /*if (_cameraBehaviour.IsCameraMovementUnavailable())
            {
                ChangeState<CameraIdleState>();
                return;
            }*/
            
            if (fingers.Count == 2)
            {
                _cameraBehaviour.ResetMoveData();
                ChangeState<CameraPinchZoomState>();
                return;
            }

            // Get the last and current screen point of all fingers
            var lastScreenPoint = LeanGesture.GetLastScreenCenter(fingers);
            var screenPoint = LeanGesture.GetScreenCenter(fingers);

            // Get the world delta of them after conversion
            var dragDelta = _screenDepth.ConvertDelta(screenPoint, lastScreenPoint, _cameraBehaviour.gameObject);
            var cameraMoveOffset = _cameraBehaviour.MoveOffset;
            var velocity = Mathf.Lerp(cameraMoveOffset.magnitude, dragDelta.magnitude, _cameraSettings.Smoothing);
            cameraMoveOffset += dragDelta;
            cameraMoveOffset.y = 0;

            var inertiaScale =  Time.deltaTime / TargetFrameDuration;

            var moveDelta = Vector3.Lerp(cameraMoveOffset.normalized * velocity, Vector3.zero, _cameraSettings.DragInertia * inertiaScale);

            _cameraBehaviour.ShadowPosition += moveDelta;
            _cameraBehaviour.transform.position = _cameraBehaviour.ShadowPosition;
            cameraMoveOffset -= moveDelta;
            _cameraBehaviour.MoveOffset = cameraMoveOffset;
        }

        public override void OnDragEnd()
        {
            ChangeState<CameraIdleState>();
        }
    }
}