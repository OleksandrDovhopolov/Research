using System;
using UnityEngine;
using UnityEngine.Events;

namespace Fabros.TileEditor
{
    public abstract class GenericProperty<T> : BaseProperty
    {
        [SerializeField] private T _defaultValue;
        private T _value;

        public UnityEvent<T> onValueChangeEvent = new UnityEvent<T>();

        public T GetDefaultGenericValue() => _defaultValue;
        public T GetGenericValue() => _value;

        public override void SetValue(object value)
        {
            _value = (T) Convert.ChangeType(value, typeof(T));
            onValueChangeEvent?.Invoke(_value);
        }

        public override object GetDefaultValue() => GetDefaultGenericValue();
        public override object GetValue() => GetGenericValue();
    }
}