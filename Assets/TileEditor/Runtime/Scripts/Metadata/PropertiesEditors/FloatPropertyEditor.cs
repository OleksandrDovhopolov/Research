using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class FloatPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private InputField _fieldValue;

        public void Init(string fieldName, double value, Action<float> onValueChanged)
        {
            _fieldName.text = fieldName;
            _fieldValue.text = value.ToString("0.##");
            _fieldValue.onEndEdit.AddListener(_ => { onValueChanged?.Invoke(GetFloatValue()); });
        }

        private float GetFloatValue() => Convert.ToSingle(_fieldValue.text);

        public override object GetValue() => GetFloatValue();
    }
}
