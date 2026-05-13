using System;
using UnityEngine;
using UnityEngine.Events;

namespace Fabros.TileEditor
{
    public class EnumProperty : BaseProperty
    {
        [SerializeField] private int _defaultValueIndex;
        [SerializeField] private string[] _enumValues;
        public UnityEvent<int> onValueChangeEvent = new UnityEvent<int>();

        private int _valueIndex;

        void Awake() => SetValue(0);

        public override object GetDefaultValue() => _defaultValueIndex;

        public override object GetValue() => _valueIndex;

        public string[] GetEnumValuesNames() => _enumValues;

        public string GetEnumValueName() => _enumValues[_valueIndex];

        public override void SetValue(object value)
        {
            _valueIndex = Convert.ToInt32(value);
            onValueChangeEvent?.Invoke(_valueIndex);
        }

        public void SetEnumValuesNames(string[] names) => _enumValues = names;
    }
}
