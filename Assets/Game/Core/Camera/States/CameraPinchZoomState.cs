using Lean.Touch;
using UnityEngine;

namespace CameraModule
{
    public class CameraPinchZoomState : BaseCameraState
    {
        public CameraPinchZoomState(CameraBehaviour cameraBehaviour, CameraSettings cameraSettings) : base(cameraBehaviour, cameraSettings)
        {
        }
        
        public override void LateUpdate()
        {
            // Get the fingers we want to use
            var fingers = _fingerFilter.UpdateAndGetFingers();
            if (fingers.Count == 1)
            {
                _cameraBehaviour.ScaleDelta = 0f;
                ChangeState<CameraDragState>();
                return;
            }
            if (fingers.Count == 0)
            {
                _cameraBehaviour.ScaleDelta = 0f;
                ChangeState<CameraIdleState>();
                return;
            }
            
            // Calculate the rotation values based on these fingers
            var twistDegrees = LeanGesture.GetPinchScale(fingers);
            var scaleDelta = _cameraBehaviour.ScaleDelta;
            // Changing camera zoom
            scaleDelta += (twistDegrees - 1) * _cameraSettings.ZoomSpeed;
            var delta = Mathf.Lerp(scaleDelta, 0, _cameraSettings.ZoomSmoothing);
            scaleDelta -= delta;
            _cameraBehaviour.ScaleDelta = scaleDelta;
            
            var screenPoint = _screenDepth.Convert(LeanGesture.GetScreenCenter(fingers));
            var maxScale = _cameraBehaviour.MaxScale;
            var minScale = _cameraBehaviour.MinScale;
            
            _cameraBehaviour.OrthographicSize = Mathf.Clamp(_cameraBehaviour.OrthographicSize - delta, maxScale, minScale);

            _cameraBehaviour.transform.position += screenPoint - _screenDepth.Convert(LeanGesture.GetScreenCenter(fingers));
            
            //_cameraBehaviour.UpdateFocusBorder();
            //_cameraBehaviour.PositionFix();
        }
        
        public override void OnDragEnd()
        {
            _cameraBehaviour.ScaleDelta = 0f;
            ChangeState<CameraIdleState>();
        }
    }
}