using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class FloatRangedPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private Slider _sliderValue;
        [SerializeField] private InputField _fieldValue;

        public void Init(string fieldName, float minValue, float maxValue, float value, Action<float> onValueChanged)
        {
            _fieldName.text = fieldName;
            UpdateText(value);

            _sliderValue.minValue = minValue;
            _sliderValue.maxValue = maxValue;
            _sliderValue.value = value;
            _sliderValue.onValueChanged.AddListener(v =>
            {
                onValueChanged?.Invoke(v);
                UpdateText(v);
            });

            _fieldValue.onEndEdit.AddListener(text =>
            {
                var clampedValue = Mathf.Clamp(Convert.ToSingle(text), _sliderValue.minValue, _sliderValue.maxValue);
                UpdateText(clampedValue);
                _sliderValue.SetValueWithoutNotify(clampedValue);
                onValueChanged?.Invoke(clampedValue);
            });
        }

        private void UpdateText(float value) => _fieldValue.text = value.ToString("0.##");

        public override object GetValue() => _sliderValue.value;
    }
}
