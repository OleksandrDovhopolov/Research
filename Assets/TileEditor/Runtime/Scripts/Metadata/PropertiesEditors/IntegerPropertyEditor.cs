using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class IntegerPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private InputField _fieldValue;

        public void Init(string fieldName, int value, Action<int> onValueChanged)
        {
            _fieldName.text = fieldName;
            _fieldValue.text = value.ToString();
            _fieldValue.onEndEdit.AddListener(_ => { onValueChanged.Invoke(GetIntValue()); });
        }

        private int GetIntValue() => Convert.ToInt32(_fieldValue.text);

        public override object GetValue() => GetIntValue();
    }
}
