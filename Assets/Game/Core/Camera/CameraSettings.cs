using UnityEngine;

namespace CameraModule
{
    [CreateAssetMenu(fileName = "CameraSettings", menuName = "CameraModule/Create Settings")]
    public class CameraSettings : ScriptableObject
    {
        public float ZoomMin = 3f;
        public float ZoomMax = 0.6f;
        public float ZoomMultiplier = 1f;
        public float ZoomSpeed = 1f;
        public float OneFingerZoomSpeed = 0.1f;
        public float ZoomSmoothing = 0.12f;

        public float DragInertia;
        public float StopInertia;
        public float Smoothing;

        public float CameraBordersOffsetX;
        public float CameraBordersOffsetY;
        public float CameraBordersSpeedFactorX;
        public float CameraBordersSpeedFactorY;
        public float BorderCameraSpeedMin;
        public float BorderCameraSpeedMax;

        public float CameraFocusDuration;
    }
}