using TileEditor;
using UnityEngine;

namespace Game.Features.Locations
{
    [DisallowMultipleComponent]
    public sealed class LocationInteractableView : MonoBehaviour, ILocationInteractable
    {
        [SerializeField] private string _interactionId;
        [SerializeField] private string _interactionKey;
        [SerializeField] private int _priority;
        [SerializeField] private bool _isInteractionEnabled = true;
        [SerializeField] private Transform _hudAnchor;
        [SerializeField] private Vector3 _hudOffset;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private bool _colliderIsTrigger = true;
        [SerializeField] private GameObject _runtimeVisualRoot;
        [SerializeField] private bool _hideVisualInRuntime;

        private LocationObject _locationObject;
        private bool _isEditorMode = true;

        public string InteractionId => _interactionId;
        public string InteractionKey => _interactionKey;
        public Transform HudAnchor => EnsureHudAnchor();
        public int Priority => _priority;
        public bool IsInteractionEnabled => _isInteractionEnabled;

        private void Awake()
        {
            ResolveLocationObject();
            SubscribeToLocationObject();
            ApplyCollider();
            ApplyHudAnchor();
        }

        private void OnDestroy()
        {
            if (_locationObject != null)
                _locationObject.OnInitDone -= InitializeFromLocationObject;
        }

        public void InitializeFromLocationObject(bool isEditor)
        {
            _isEditorMode = isEditor;
            ResolveLocationObject();

            if (_locationObject != null)
            {
                ReadMetadata(_locationObject);
                if (string.IsNullOrWhiteSpace(_interactionId))
                    _interactionId = string.IsNullOrWhiteSpace(_locationObject.InstanceId)
                        ? _locationObject.Uid
                        : _locationObject.InstanceId;
            }

            ApplyCollider();
            ApplyHudAnchor();
            ApplyRuntimeVisualState(isEditor);
        }

        public void SetInteractionId(string interactionId)
        {
            _interactionId = interactionId;
        }

        public void SetInteractionKey(string interactionKey)
        {
            _interactionKey = interactionKey;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _isInteractionEnabled = isEnabled;
            ApplyCollider();
        }

        private void ReadMetadata(LocationObject locationObject)
        {
            if (locationObject.TryParseProperty<string>(LocationInteractionPropertyNames.InteractionId, out var interactionId))
                _interactionId = interactionId;

            if (locationObject.TryParseProperty<string>(LocationInteractionPropertyNames.InteractionKey, out var interactionKey))
                _interactionKey = interactionKey;

            if (locationObject.TryParseProperty<bool>(LocationInteractionPropertyNames.IsInteractionEnabled, out var isEnabled))
                _isInteractionEnabled = isEnabled;

            if (locationObject.TryParseProperty<int>(LocationInteractionPropertyNames.Priority, out var priority))
                _priority = priority;
        }

        private void ResolveLocationObject()
        {
            if (_locationObject == null)
                _locationObject = GetComponentInParent<LocationObject>();
        }

        private void SubscribeToLocationObject()
        {
            if (_locationObject == null)
                return;

            _locationObject.OnInitDone -= InitializeFromLocationObject;
            _locationObject.OnInitDone += InitializeFromLocationObject;
        }

        private void ApplyCollider()
        {
            if (_collider == null)
                _collider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();

            _collider.isTrigger = _colliderIsTrigger;
            _collider.enabled = _isInteractionEnabled && !_isEditorMode;
        }

        private void ApplyHudAnchor()
        {
            EnsureHudAnchor().localPosition = _hudOffset;
        }

        private Transform EnsureHudAnchor()
        {
            if (_hudAnchor != null)
                return _hudAnchor;

            var anchor = new GameObject("HudAnchor");
            anchor.transform.SetParent(transform, false);
            _hudAnchor = anchor.transform;
            return _hudAnchor;
        }

        private void ApplyRuntimeVisualState(bool isEditor)
        {
            if (isEditor || !_hideVisualInRuntime || _runtimeVisualRoot == null)
                return;

            _runtimeVisualRoot.SetActive(false);
        }
    }
}
