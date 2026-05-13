using System;
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
        private LocationInteractionRouter _locationInteractionRouter;

        [Inject]
        public void Construct(LocationInteractionRouter locationInteractionRouter)
        {
            _locationInteractionRouter = locationInteractionRouter ?? throw new ArgumentNullException(nameof(locationInteractionRouter));
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
            if (_interactable != null)
            {
                _locationInteractionRouter?.Route(_interactable, _target.position);
                return;
            }

            Debug.LogWarning($"[ZoneInfoHud] Click key='{_interactable?.InteractionKey}', id='{_interactable?.InteractionId}'. Fishing logger is not registered.");
        }
    }
}
