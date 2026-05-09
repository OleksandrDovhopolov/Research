using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace UIShared
{
    public sealed class HudMissTapInputController : ITickable
    {
        private readonly HashSet<IRectMissTap> _rectMissTaps = new();
        private readonly List<IRectMissTap> _iterationBuffer = new();

        public void Tick()
        {
            if (_rectMissTaps.Count == 0)
                return;

            if (TryGetBeganPointerPosition(out var pointerPosition))
                HandleTap(pointerPosition);
        }

        public void AddHud(IRectMissTap hudTouch)
        {
            if (hudTouch != null)
                _rectMissTaps.Add(hudTouch);
        }

        public void RemoveHud(IRectMissTap hudTouch)
        {
            if (hudTouch != null)
                _rectMissTaps.Remove(hudTouch);
        }

        public void CloseAllHud()
        {
            _iterationBuffer.Clear();
            _iterationBuffer.AddRange(_rectMissTaps);

            for (var i = _iterationBuffer.Count - 1; i >= 0; i--)
                _iterationBuffer[i]?.OnMissTap();

            _iterationBuffer.Clear();
        }

        private void HandleTap(Vector2 pointerPosition)
        {
            _iterationBuffer.Clear();
            _iterationBuffer.AddRange(_rectMissTaps);

            for (var i = _iterationBuffer.Count - 1; i >= 0; i--)
            {
                var rectMissTap = _iterationBuffer[i];
                if (rectMissTap == null)
                    continue;

                if (ContainsPointer(rectMissTap, pointerPosition))
                    continue;

                rectMissTap.OnMissTap();
            }

            _iterationBuffer.Clear();
        }

        private static bool ContainsPointer(IRectMissTap rectMissTap, Vector2 pointerPosition)
        {
            var rectTransforms = rectMissTap.GetRectTransform();
            if (rectTransforms == null)
                return false;

            foreach (var rectTransform in rectTransforms)
            {
                if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
                    continue;

                var eventCamera = ResolveEventCamera(rectTransform);
                if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition, eventCamera))
                    return true;
            }

            return false;
        }

        private static Camera ResolveEventCamera(RectTransform rectTransform)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
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

        private static bool TryGetBeganPointerPosition(out Vector2 position)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                position = touch.position;
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = default;
            return false;
        }
    }
}
