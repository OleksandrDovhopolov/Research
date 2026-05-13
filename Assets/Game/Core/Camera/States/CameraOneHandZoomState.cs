using Lean.Touch;
using UnityEngine;

namespace CameraModule
{
    public class CameraOneHandZoomState : BaseCameraState
    {
        public CameraOneHandZoomState(CameraBehaviour cameraBehaviour, CameraSettings cameraSettings) : base(cameraBehaviour, cameraSettings)
        {
        }

        public override void LateUpdate()
        {
            // Get the fingers we want to use
            var fingers = _fingerFilter.UpdateAndGetFingers();

            if (fingers.Count == 2)
            {
                _cameraBehaviour.ScaleDelta = 0f;
                ChangeState<CameraPinchZoomState>();
                return;
            }
            
            var lastScreenPoint = LeanGesture.GetLastScreenCenter(fingers);
            var screenPoint = LeanGesture.GetScreenCenter(fingers);

            var dragDelta = _screenDepth.ConvertDelta(screenPoint, lastScreenPoint, _cameraBehaviour.gameObject);
 
            var scaleDelta = _cameraBehaviour.ScaleDelta;
            scaleDelta += dragDelta.z * _cameraSettings.OneFingerZoomSpeed;
            var delta = Mathf.Lerp(scaleDelta, 0, _cameraSettings.ZoomSmoothing);
            scaleDelta -= delta;
            _cameraBehaviour.ScaleDelta = scaleDelta;
            
            var maxScale = _cameraBehaviour.MaxScale;
            var minScale = _cameraBehaviour.MinScale;
            
            _cameraBehaviour.OrthographicSize = Mathf.Clamp(_cameraBehaviour.OrthographicSize - delta, maxScale, minScale);
            
            //_cameraBehaviour.UpdateFocusBorder();
            //_cameraBehaviour.PositionFix();
        }

        public override void OnDragEnd()
        {
            _cameraBehaviour.ScaleDelta = 0f;
            _cameraBehaviour.ResetMoveData();
            ChangeState<CameraIdleState>();
        }
    }
}