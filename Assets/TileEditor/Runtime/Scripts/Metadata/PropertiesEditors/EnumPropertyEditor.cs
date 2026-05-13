using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class EnumPropertyEditor : BasePropertyEditor
    {
        [SerializeField] private Dropdown _dropdown;

        public void Init(string fieldName, string[] values, int value, Action<int> onValueChanged)
        {
            _fieldName.text = fieldName;

            _dropdown.AddOptions(values.ToList());
            _dropdown.value = value;
            _dropdown.onValueChanged.AddListener(v => { onValueChanged?.Invoke(v);});
        }

        public override object GetValue() => _dropdown.value;
    }
}
