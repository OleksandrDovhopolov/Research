using System;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Fishing
{
    public sealed class LocationZoneInfoHudItemView : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private Transform _target;
        private ILocationInteractable _interactable;
        private IFishingZoneInfoLogger _zoneInfoLogger;
        private ILocationFishingZoneIdResolver _zoneIdResolver;
        private IFishingLureSelectionHudFacade _fishingLureSelectionHudFacade;

        [Inject]
        public void Construct(IFishingZoneInfoLogger zoneInfoLogger, ILocationFishingZoneIdResolver zoneIdResolver, IFishingLureSelectionHudFacade fishingLureSelectionHudFacade)
        {
            _zoneInfoLogger = zoneInfoLogger;
            _zoneIdResolver = zoneIdResolver;
            _fishingLureSelectionHudFacade = fishingLureSelectionHudFacade ?? throw new ArgumentNullException(nameof(fishingLureSelectionHudFacade));
        }

        public void Initialize(ILocationInteractable interactable, string label)
        {
            ResolveReferences();

            _interactable = interactable;
            _target = interactable.HudAnchor;

            if (_label != null)
                _label.text = label ?? string.Empty;

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
                _button.onClick.AddListener(HandleClicked);
            }

            UpdateTransform();
        }

        private void LateUpdate()
        {
            UpdateTransform();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        private void UpdateTransform()
        {
            if (_target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            transform.position = _target.position;
            HudCameraFacingUtility.FaceCamera(transform, _canvas);
        }

        private void ResolveReferences()
        {
            _canvas ??= GetComponent<Canvas>();
            _button ??= GetComponentInChildren<Button>(true);
        }

        private void HandleClicked()
        {
            if (_zoneInfoLogger != null)
            {
                _zoneInfoLogger.LogZoneInfoAsync(_zoneIdResolver.ResolveZoneId(_interactable)).Forget();
                return;
            }

            Debug.LogWarning($"[ZoneInfoHud] Click key='{_interactable?.InteractionKey}', id='{_interactable?.InteractionId}'. Fishing logger is not registered.");
        }
    }
}
