using UnityEngine;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    public abstract class EnumPropertyHelper : MonoBehaviour
    {
        [SerializeField] private string _propertyName = default;
        [SerializeField] private string[] _propertyValues = default;

        private void Awake()
        {
            var enumProperty = gameObject.AddComponent<EnumProperty>();
            enumProperty.SetPropertyName(_propertyName);
            enumProperty.SetEnumValuesNames(_propertyValues);
            enumProperty.onValueChangeEvent.AddListener(OnValueChange);

            enumProperty.SetValue(0);
        }

        protected abstract void OnValueChange(int id);
    }
}
