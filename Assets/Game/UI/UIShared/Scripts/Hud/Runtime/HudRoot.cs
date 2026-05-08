using System;
using UnityEngine;

namespace UIShared
{
    public sealed class HudRoot : MonoBehaviour
    {
        private const string ScreenLayerName = "ScreenHudLayer";
        private const string WorldLayerName = "WorldHudLayer";
        private const string OverlayLayerName = "OverlayHudLayer";
        private const string DebugLayerName = "DebugHudLayer";

        [SerializeField] private Transform _screenLayer;
        [SerializeField] private Transform _worldLayer;
        [SerializeField] private Transform _overlayLayer;
        [SerializeField] private Transform _debugLayer;

        public Transform GetLayerRoot(HudLayer layer)
        {
            return layer switch
            {
                HudLayer.Screen => GetOrCreateLayer(ref _screenLayer, ScreenLayerName),
                HudLayer.World => GetOrCreateLayer(ref _worldLayer, WorldLayerName),
                HudLayer.Overlay => GetOrCreateLayer(ref _overlayLayer, OverlayLayerName),
                HudLayer.Debug => GetOrCreateLayer(ref _debugLayer, DebugLayerName),
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
            };
        }

        private Transform GetOrCreateLayer(ref Transform layerRoot, string layerName)
        {
            if (layerRoot != null)
                return layerRoot;

            var existing = transform.Find(layerName);
            if (existing != null)
            {
                layerRoot = existing;
                return layerRoot;
            }

            var layerObject = transform is RectTransform
                ? new GameObject(layerName, typeof(RectTransform))
                : new GameObject(layerName);

            layerRoot = layerObject.transform;
            layerRoot.SetParent(transform, false);
            ResetTransform(layerRoot);

            return layerRoot;
        }

        private static void ResetTransform(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            if (target is RectTransform rectTransform)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }
    }
}
