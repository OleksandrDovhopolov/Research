using System;
using Game.Features.Locations;
using UnityEngine;
using UnityEngine.Events;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    [RequireComponent(typeof(LocationInteractableView))]
    public class LocationInteractionPropertyGenerator : MonoBehaviour
    {
        [SerializeField] protected string _defaultInteractionId;
        [SerializeField] private LocationInteractionType _defaultInteractionType = LocationInteractionType.Custom;
        [SerializeField] private bool _defaultInteractionEnabled = true;
        [SerializeField] private int _defaultPriority;

        [Header("HUD")]
        [SerializeField] private Vector3 _defaultHudOffset;
        [SerializeField] private Vector2 _hudOffsetXLimits = new(-10f, 10f);
        [SerializeField] private Vector2 _hudOffsetYLimits = new(-10f, 10f);
        [SerializeField] private Vector2 _hudOffsetZLimits = new(-10f, 10f);

        [Header("Collider")]
        [SerializeField] private Vector3 _defaultColliderOffset = new(0f, 0.5f, 0f);
        [SerializeField] private Vector3 _defaultColliderSize = Vector3.one;
        [SerializeField] private Vector2 _colliderOffsetXLimits = new(-10f, 10f);
        [SerializeField] private Vector2 _colliderOffsetYLimits = new(-10f, 10f);
        [SerializeField] private Vector2 _colliderOffsetZLimits = new(-10f, 10f);
        [SerializeField] private Vector2 _colliderSizeXLimits = new(0.1f, 20f);
        [SerializeField] private Vector2 _colliderSizeYLimits = new(0.1f, 20f);
        [SerializeField] private Vector2 _colliderSizeZLimits = new(0.1f, 20f);

        protected LocationInteractableView View { get; private set; }

        protected virtual string DefaultInteractionId => _defaultInteractionId;
        protected virtual LocationInteractionType DefaultInteractionType => _defaultInteractionType;

        protected virtual void Awake()
        {
            View = GetComponent<LocationInteractableView>();

            CreateStringProperty(
                LocationInteractionPropertyNames.InteractionId,
                DefaultInteractionId,
                View.SetInteractionId);

            CreateInteractionTypeProperty();

            CreateBooleanProperty(
                LocationInteractionPropertyNames.IsInteractionEnabled,
                _defaultInteractionEnabled,
                View.SetInteractionEnabled);

            CreateIntegerProperty(
                LocationInteractionPropertyNames.Priority,
                _defaultPriority,
                View.SetPriority);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.HudOffsetX,
                _hudOffsetXLimits,
                _defaultHudOffset.x,
                View.SetHudOffsetX);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.HudOffsetY,
                _hudOffsetYLimits,
                _defaultHudOffset.y,
                View.SetHudOffsetY);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.HudOffsetZ,
                _hudOffsetZLimits,
                _defaultHudOffset.z,
                View.SetHudOffsetZ);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderOffsetX,
                _colliderOffsetXLimits,
                _defaultColliderOffset.x,
                View.SetColliderOffsetX);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderOffsetY,
                _colliderOffsetYLimits,
                _defaultColliderOffset.y,
                View.SetColliderOffsetY);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderOffsetZ,
                _colliderOffsetZLimits,
                _defaultColliderOffset.z,
                View.SetColliderOffsetZ);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderSizeX,
                _colliderSizeXLimits,
                _defaultColliderSize.x,
                View.SetColliderSizeX);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderSizeY,
                _colliderSizeYLimits,
                _defaultColliderSize.y,
                View.SetColliderSizeY);

            CreateFloatRangedProperty(
                LocationInteractionPropertyNames.ColliderSizeZ,
                _colliderSizeZLimits,
                _defaultColliderSize.z,
                View.SetColliderSizeZ);
        }

        protected StringProperty CreateStringProperty(string propertyName, string defaultValue, UnityAction<string> onValueChanged)
        {
            var property = gameObject.AddComponent<StringProperty>();
            property.SetPropertyName(propertyName);
            property.onValueChangeEvent.AddListener(onValueChanged);
            property.SetValue(defaultValue ?? string.Empty);
            return property;
        }

        protected BooleanProperty CreateBooleanProperty(string propertyName, bool defaultValue, UnityAction<bool> onValueChanged)
        {
            var property = gameObject.AddComponent<BooleanProperty>();
            property.SetPropertyName(propertyName);
            property.onValueChangeEvent.AddListener(onValueChanged);
            property.SetValue(defaultValue);
            return property;
        }

        protected IntegerProperty CreateIntegerProperty(string propertyName, int defaultValue, UnityAction<int> onValueChanged)
        {
            var property = gameObject.AddComponent<IntegerProperty>();
            property.SetPropertyName(propertyName);
            property.onValueChangeEvent.AddListener(onValueChanged);
            property.SetValue(defaultValue);
            return property;
        }

        protected FloatRangedProperty CreateFloatRangedProperty(string propertyName, Vector2 limits, float defaultValue, UnityAction<float> onValueChanged)
        {
            var property = gameObject.AddComponent<FloatRangedProperty>();
            property.SetPropertyName(propertyName);
            property.SetMinValue(limits.x);
            property.SetMaxValue(limits.y);
            property.onValueChangeEvent.AddListener(onValueChanged);
            property.SetValue(defaultValue);
            return property;
        }

        private void CreateInteractionTypeProperty()
        {
            var property = gameObject.AddComponent<EnumProperty>();
            property.SetPropertyName(LocationInteractionPropertyNames.InteractionType);
            property.SetEnumValuesNames(Enum.GetNames(typeof(LocationInteractionType)));
            property.onValueChangeEvent.AddListener(index => View.SetInteractionType((LocationInteractionType)index));
            property.SetValue((int)DefaultInteractionType);
        }
    }
}
