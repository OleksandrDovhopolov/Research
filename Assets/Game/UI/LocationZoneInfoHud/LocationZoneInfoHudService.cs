using System;
using Game.Features.Locations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public sealed class LocationZoneInfoHudService : IDisposable
    {
        private const string RootName = "LocationZoneInfoHudRoot";
        private const float WorldScale = 0.01f;

        private readonly LocationZoneInfoHudDefinitionRegistry _registry;

        private MainLocationBootstrap _locationBootstrap;
        private GameObject _rootObject;
        private Canvas _canvas;
        private Sprite _buttonSprite;
        private TMP_FontAsset _fontAsset;
        private bool _isInitialized;

        public LocationZoneInfoHudService(LocationZoneInfoHudDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public void Initialize(MonoBehaviour locationBootstrap)
        {
            _locationBootstrap = locationBootstrap as MainLocationBootstrap;

            if (_locationBootstrap == null && locationBootstrap != null)
            {
                Debug.LogWarning($"[ZoneInfoHud] Unsupported location bootstrap type '{locationBootstrap.GetType().Name}'.");
            }
        }

        public bool TryInitialize()
        {
            if (_isInitialized)
                return true;

            if (_locationBootstrap == null || _locationBootstrap.CurrentLocation == null)
                return false;

            BuildRoot();
            SpawnItems();

            _isInitialized = true;
            return true;
        }

        public void Dispose()
        {
            if (_rootObject != null)
                UnityEngine.Object.Destroy(_rootObject);

            _rootObject = null;
            _canvas = null;
            _locationBootstrap = null;
            _isInitialized = false;
        }

        private void BuildRoot()
        {
            if (_rootObject != null)
                return;

            _buttonSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            _fontAsset = TMP_Settings.defaultFontAsset;

            _rootObject = new GameObject(RootName, typeof(RectTransform));
            _rootObject.transform.position = Vector3.zero;
            _rootObject.transform.rotation = Quaternion.identity;
            //_rootObject.transform.localScale = Vector3.one * WorldScale;

            //_canvas = _rootObject.AddComponent<Canvas>();
            //_canvas.renderMode = RenderMode.WorldSpace;
            //_canvas.worldCamera = Camera.main;

            //_rootObject.AddComponent<GraphicRaycaster>();
            _rootObject.AddComponent<LocationZoneInfoHudCanvasMarker>();
        }

        private void SpawnItems()
        {
            foreach (var locationObject in _locationBootstrap.CurrentLocation.IterateObjects())
            {
                var interactable = locationObject != null ? locationObject.GetComponent<LocationInteractableView>() : null;
                if (interactable == null || !interactable.IsInteractionEnabled)
                    continue;

                if (!_registry.TryGetDefinition(interactable.InteractionKey, out var definition) || !definition.IsEnabled)
                    continue;

                if (interactable.HudAnchor == null)
                    continue;

                CreateItem(interactable, definition);
            }
        }

        private void CreateItem(ILocationInteractable interactable, LocationZoneInfoHudDefinition definition)
        {
            var itemObject = new GameObject($"ZoneInfo_{interactable.InteractionId}");
            itemObject.transform.SetParent(_rootObject.transform, false);

            var itemView = itemObject.AddComponent<LocationZoneInfoHudItemView>();
            itemView.Initialize(interactable, definition.Label, _canvas, _buttonSprite, _fontAsset);
        }
    }
}
