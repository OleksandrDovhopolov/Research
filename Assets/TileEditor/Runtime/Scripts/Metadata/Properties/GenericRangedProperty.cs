using System;
using UnityEngine;

namespace Fabros.TileEditor
{
    public abstract class GenericRangedProperty<T> : GenericProperty<T>
    {
        [SerializeField] private T _minValue;
        [SerializeField] private T _maxValue;

        public T GetMinValue() => _minValue;
        public T GetMaxValue() => _maxValue;

        public T SetMinValue(object value) => _minValue = (T)Convert.ChangeType(value, typeof(T));
        public T SetMaxValue(object value) => _maxValue = (T)Convert.ChangeType(value, typeof(T));
    }
}