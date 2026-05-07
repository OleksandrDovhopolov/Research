using UnityEngine;

namespace Fabros.TileEditor
{
    public abstract class BaseProperty : MonoBehaviour
    {
        [SerializeField] protected string _propertyName;

        void Awake() => SetValue(GetDefaultValue());

        public string GetName() => _propertyName;

        public abstract object GetDefaultValue();
        public abstract object GetValue();
        public abstract void SetValue(object value);

        public void SetPropertyName(string newName) => _propertyName = newName;
    }
}