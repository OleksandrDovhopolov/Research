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
        [SerializeField] private string _defaultInteractionKey = LocationInteractionKeys.Custom;
        [SerializeField] private bool _defaultInteractionEnabled = true;

        protected LocationInteractableView View { get; private set; }

        protected virtual string DefaultInteractionId => _defaultInteractionId;
        protected virtual string DefaultInteractionKey => _defaultInteractionKey;

        protected virtual void Awake()
        {
            View = GetComponent<LocationInteractableView>();

            CreateStringProperty(
                LocationInteractionPropertyNames.InteractionId,
                DefaultInteractionId,
                View.SetInteractionId);

            CreateStringProperty(
                LocationInteractionPropertyNames.InteractionKey,
                DefaultInteractionKey,
                View.SetInteractionKey);

            CreateBooleanProperty(
                LocationInteractionPropertyNames.IsInteractionEnabled,
                _defaultInteractionEnabled,
                View.SetInteractionEnabled);
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
    }
}
