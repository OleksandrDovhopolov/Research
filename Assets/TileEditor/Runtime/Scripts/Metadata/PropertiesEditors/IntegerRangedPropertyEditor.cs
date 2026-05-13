using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class IntegerRangedPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private Slider _sliderValue;
        [SerializeField] private Text _textValue;

        public void Init(string fieldName, int minValue, int maxValue, int value, Action<int> onValueChanged)
        {
            _fieldName.text = fieldName;
            UpdateText(value);

            _sliderValue.minValue = minValue;
            _sliderValue.maxValue = maxValue;
            _sliderValue.value = value;
            _sliderValue.onValueChanged.AddListener(v =>
            {
                var intValue = (int)v;
                onValueChanged?.Invoke(intValue);
                UpdateText(intValue);
            });
        }

        private void UpdateText(int value) => _textValue.text = value.ToString();

        private float GetIntegerValue() => Convert.ToInt32(_sliderValue.value);

        public override object GetValue() => GetIntegerValue();
    }
}
