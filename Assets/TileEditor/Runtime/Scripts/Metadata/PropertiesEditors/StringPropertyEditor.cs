using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class StringPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private InputField _fieldValue;

        public void Init(string fieldName, string value, Action<string> onValueChanged)
        {
            _fieldName.text = fieldName;
            _fieldValue.text = value;
            _fieldValue.onEndEdit.AddListener(_ => { onValueChanged?.Invoke(_fieldValue.text); });
        }

        public override object GetValue() => _fieldValue.text;
    }
}