using UnityEngine;

namespace Game.Fishing
{
    public class DropUITarget : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;

        public bool IsPositionInsideRect(Vector2 screenPosition)
        {
            if (_rectTransform == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                screenPosition,
                ResolveEventCamera());
        }

        private Camera ResolveEventCamera()
        {
            var canvas = _rectTransform != null
                ? _rectTransform.GetComponentInParent<Canvas>()
                : null;

            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            var canvasCamera = canvas.worldCamera;
            if (canvasCamera != null && canvasCamera.isActiveAndEnabled)
                return canvasCamera;

            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                canvas.worldCamera = mainCamera;
                return mainCamera;
            }

            return null;
        }
    }
}
