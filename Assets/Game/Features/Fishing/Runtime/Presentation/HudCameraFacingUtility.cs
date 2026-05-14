using UnityEngine;

namespace Game.Fishing
{
    public static class HudCameraFacingUtility
    {
        public static void FaceCamera(Transform targetTransform, Canvas canvas)
        {
            if (targetTransform == null)
                return;

            var targetCamera = ResolveCamera(canvas);
            if (targetCamera == null)
                return;

            targetTransform.rotation = targetCamera.transform.rotation;
        }

        public static Camera ResolveCamera(Canvas canvas)
        {
            var mainCamera = Camera.main;

            if (canvas != null)
            {
                var canvasCamera = canvas.worldCamera;
                if (canvasCamera != null && canvasCamera.isActiveAndEnabled)
                    return canvasCamera;

                if (mainCamera != null && mainCamera.isActiveAndEnabled)
                    canvas.worldCamera = mainCamera;
            }

            return mainCamera != null && mainCamera.isActiveAndEnabled
                ? mainCamera
                : null;
        }
    }
}
