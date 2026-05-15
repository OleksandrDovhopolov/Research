using System.Linq;
using System.Reflection;
using Game.Features.Locations;
using UnityEngine;
using UnityEngine.Events;

namespace TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    [RequireComponent(typeof(LocationInteractableView))]
    public class LocationInteractionPropertyGenerator : MonoBehaviour
    {
        private static readonly string[] InteractionKeys = typeof(LocationInteractionKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .OrderBy(field => (string)field.GetRawConstantValue() == LocationInteractionKeys.Custom ? 0 : 1)
            .ThenBy(field => field.MetadataToken)
            .Select(field => (string)field.GetRawConstantValue())
            .ToArray();

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

            CreateStringOptionsProperty(
                LocationInteractionPropertyNames.InteractionKey,
                DefaultInteractionKey,
                InteractionKeys,
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

        protected StringOptionsProperty CreateStringOptionsProperty(
            string propertyName,
            string defaultValue,
            string[] options,
            UnityAction<string> onValueChanged)
        {
            var property = gameObject.AddComponent<StringOptionsProperty>();
            property.SetPropertyName(propertyName);
            property.SetOptions(options);
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
