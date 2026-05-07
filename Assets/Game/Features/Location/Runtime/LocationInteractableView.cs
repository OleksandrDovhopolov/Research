using Fabros.TileEditor;
using UnityEngine;

namespace Game.Features.Locations
{
    [DisallowMultipleComponent]
    public sealed class LocationInteractableView : MonoBehaviour, ILocationInteractable
    {
        private const float MinColliderSize = 0.01f;

        [SerializeField] private string _interactionId;
        [SerializeField] private LocationInteractionType _interactionType = LocationInteractionType.Custom;
        [SerializeField] private int _priority;
        [SerializeField] private bool _isInteractionEnabled = true;
        [SerializeField] private Transform _hudAnchor;
        [SerializeField] private Vector3 _hudOffset;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private bool _colliderIsTrigger = true;
        [SerializeField] private Vector3 _colliderOffset = new(0f, 0.5f, 0f);
        [SerializeField] private Vector3 _colliderSize = Vector3.one;
        [SerializeField] private GameObject _runtimeVisualRoot;
        [SerializeField] private bool _hideVisualInRuntime;
        [SerializeField] private string _fishingConfigId;

        private LocationObject _locationObject;

        public string InteractionId => _interactionId;
        public LocationInteractionType InteractionType => _interactionType;
        public Transform HudAnchor => EnsureHudAnchor();
        public int Priority => _priority;
        public bool IsInteractionEnabled => _isInteractionEnabled;
        public string FishingConfigId => _fishingConfigId;

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

        public void SetInteractionType(LocationInteractionType interactionType)
        {
            _interactionType = interactionType;
        }

        public void SetPriority(int priority)
        {
            _priority = priority;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _isInteractionEnabled = isEnabled;
            ApplyCollider();
        }

        public void SetHudOffsetX(float value)
        {
            _hudOffset.x = value;
            ApplyHudAnchor();
        }

        public void SetHudOffsetY(float value)
        {
            _hudOffset.y = value;
            ApplyHudAnchor();
        }

        public void SetHudOffsetZ(float value)
        {
            _hudOffset.z = value;
            ApplyHudAnchor();
        }

        public void SetColliderOffsetX(float value)
        {
            _colliderOffset.x = value;
            ApplyCollider();
        }

        public void SetColliderOffsetY(float value)
        {
            _colliderOffset.y = value;
            ApplyCollider();
        }

        public void SetColliderOffsetZ(float value)
        {
            _colliderOffset.z = value;
            ApplyCollider();
        }

        public void SetColliderSizeX(float value)
        {
            _colliderSize.x = Mathf.Max(MinColliderSize, value);
            ApplyCollider();
        }

        public void SetColliderSizeY(float value)
        {
            _colliderSize.y = Mathf.Max(MinColliderSize, value);
            ApplyCollider();
        }

        public void SetColliderSizeZ(float value)
        {
            _colliderSize.z = Mathf.Max(MinColliderSize, value);
            ApplyCollider();
        }

        public void SetFishingConfigId(string fishingConfigId)
        {
            _fishingConfigId = fishingConfigId;
        }

        private void ReadMetadata(LocationObject locationObject)
        {
            if (locationObject.TryParseProperty<string>(LocationInteractionPropertyNames.InteractionId, out var interactionId))
                _interactionId = interactionId;

            if (locationObject.TryParseEnumProperty<LocationInteractionType>(LocationInteractionPropertyNames.InteractionType, out var interactionType))
                _interactionType = interactionType;

            if (locationObject.TryParseProperty<bool>(LocationInteractionPropertyNames.IsInteractionEnabled, out var isEnabled))
                _isInteractionEnabled = isEnabled;

            if (locationObject.TryParseProperty<int>(LocationInteractionPropertyNames.Priority, out var priority))
                _priority = priority;

            TryReadVector3(
                locationObject,
                LocationInteractionPropertyNames.HudOffsetX,
                LocationInteractionPropertyNames.HudOffsetY,
                LocationInteractionPropertyNames.HudOffsetZ,
                ref _hudOffset);

            TryReadVector3(
                locationObject,
                LocationInteractionPropertyNames.ColliderOffsetX,
                LocationInteractionPropertyNames.ColliderOffsetY,
                LocationInteractionPropertyNames.ColliderOffsetZ,
                ref _colliderOffset);

            TryReadVector3(
                locationObject,
                LocationInteractionPropertyNames.ColliderSizeX,
                LocationInteractionPropertyNames.ColliderSizeY,
                LocationInteractionPropertyNames.ColliderSizeZ,
                ref _colliderSize);

            _colliderSize.x = Mathf.Max(MinColliderSize, _colliderSize.x);
            _colliderSize.y = Mathf.Max(MinColliderSize, _colliderSize.y);
            _colliderSize.z = Mathf.Max(MinColliderSize, _colliderSize.z);

            if (locationObject.TryParseProperty<string>(LocationInteractionPropertyNames.FishingConfigId, out var fishingConfigId))
                _fishingConfigId = fishingConfigId;
        }

        private static void TryReadVector3(LocationObject locationObject, string xName, string yName, string zName, ref Vector3 target)
        {
            if (locationObject.TryParseProperty<float>(xName, out var x))
                target.x = x;

            if (locationObject.TryParseProperty<float>(yName, out var y))
                target.y = y;

            if (locationObject.TryParseProperty<float>(zName, out var z))
                target.z = z;
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

            _collider.center = _colliderOffset;
            _collider.size = _colliderSize;
            _collider.isTrigger = _colliderIsTrigger;
            _collider.enabled = _isInteractionEnabled;
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
