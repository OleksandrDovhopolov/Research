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

        private ILocationInteractable _interactable;
        private Transform _target;
        private IFishingZoneInfoLogger _zoneInfoLogger;

        [Inject]
        public void Construct(IFishingZoneInfoLogger zoneInfoLogger)
        {
            _zoneInfoLogger = zoneInfoLogger;
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

            var targetCamera = ResolveCamera();
            if (targetCamera != null)
                transform.rotation = targetCamera.transform.rotation;
        }

        private Camera ResolveCamera()
        {
            var mainCamera = Camera.main;

            if (_canvas != null)
            {
                var canvasCamera = _canvas.worldCamera;
                if (canvasCamera != null && canvasCamera.isActiveAndEnabled)
                    return canvasCamera;

                if (mainCamera != null && mainCamera.isActiveAndEnabled)
                    _canvas.worldCamera = mainCamera;
            }

            return mainCamera;
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
                _zoneInfoLogger.LogZoneInfoAsync(_interactable).Forget();
                return;
            }

            Debug.LogWarning($"[ZoneInfoHud] Click key='{_interactable?.InteractionKey}', id='{_interactable?.InteractionId}'. Fishing logger is not registered.");
        }
    }
}
