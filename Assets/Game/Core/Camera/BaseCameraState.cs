using Lean.Touch;
using Utils;

namespace CameraModule
{
    public class BaseCameraState : BaseState
    {
        protected readonly CameraBehaviour _cameraBehaviour;
        protected LeanFingerFilter _fingerFilter;
        protected LeanScreenDepth _screenDepth;
        protected CameraSettings _cameraSettings;

        protected BaseCameraState(CameraBehaviour cameraBehaviour, CameraSettings cameraSettings)
        {
            _cameraBehaviour = cameraBehaviour;
            _cameraSettings = cameraSettings;
            _fingerFilter = _cameraBehaviour.FingerFilter;
            _screenDepth = _cameraBehaviour.ScreenDepth;
        }

        public override void OnEnter()
        {
        }

        public override void OnExit()
        {
        }

        public virtual void LateUpdate()
        {
        }

        public virtual void OnDrag()
        {
        }

        public virtual void OnDragEnd()
        {
        }

        public virtual void OnDragStart()
        {
        }
    }
}