using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class LureDragPreviewView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private Image _icon;

        private Canvas _canvas;

        private void Awake()
        {
            _root ??= transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(Sprite sprite, int count)
        {
            if (_root == null)
                _root = transform as RectTransform;

            if (_icon != null)
                _icon.sprite = sprite;

            gameObject.SetActive(true);
        }

        public void MoveToScreenPosition(Vector2 screenPosition)
        {
            if (_root == null)
                _root = transform as RectTransform;

            if (_root == null)
                return;

            var parentRect = _root.parent as RectTransform;
            if (parentRect == null)
            {
                _root.position = screenPosition;
                return;
            }

            _canvas ??= GetComponentInParent<Canvas>();
            var eventCamera = ResolveEventCamera(_canvas);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, eventCamera, out var localPoint))
                _root.anchoredPosition = localPoint;
        }

        public void Hide()
        {
            if (_icon != null)
                _icon.sprite = null;

            if (_root != null)
                _root.anchoredPosition = Vector2.zero;

            gameObject.SetActive(false);
        }

        private static Camera ResolveEventCamera(Canvas canvas)
        {
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
