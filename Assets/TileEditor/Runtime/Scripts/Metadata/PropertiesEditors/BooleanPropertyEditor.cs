using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class BooleanPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private Toggle _toggle;

        public void Init(string fieldName, bool value, Action<bool> onValueChanged)
        {
            _fieldName.text = fieldName;
            _toggle.isOn = value;
            _toggle.onValueChanged.AddListener(onValueChanged.Invoke);
        }

        public override object GetValue() => _toggle.isOn;
    }
}
