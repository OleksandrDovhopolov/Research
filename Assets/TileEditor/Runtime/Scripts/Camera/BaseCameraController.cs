using UnityEngine;

namespace Fabros.TileEditor.CameraController
{
    public abstract class BaseCameraController : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------

        protected Transform _cameraTransform;
        protected Camera _camera;

        //--------------------------------------------------------------------------------------------------------------------------

        private bool _isDragActive;

        //--------------------------------------------------------------------------------------------------------------------------

        public void Init(Transform cameraTransform, Camera mainCamera)
        {
            _cameraTransform = cameraTransform;
            _camera = mainCamera;

            DoInit();
        }

        protected virtual void DoInit() { }

        //--------------------------------------------------------------------------------------------------------------------------

        public abstract void SetDefaultPosition();

        //--------------------------------------------------------------------------------------------------------------------------

        public virtual void StartCameraDrag()
        {
            StopCameraMove();
            _isDragActive = true;
        }

        public void UpdateCameraDrag()
        {
            if (!_isDragActive) return;

            DoCameraDrag();
        }

        protected abstract void DoCameraDrag();

        public virtual void StopCameraDrag()
        {
            _isDragActive = false;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public void MoveCamera(Vector3 targetPosition)
        {
            StopCameraDrag();
            StopCameraMove();

            DoCameraMove(targetPosition);
        }

        protected abstract void DoCameraMove(Vector3 targetPosition);
        protected abstract void StopCameraMove();

        //--------------------------------------------------------------------------------------------------------------------------

        public abstract void ZoomOut();
        public abstract void ZoomIn();

        //--------------------------------------------------------------------------------------------------------------------------
    }
}

