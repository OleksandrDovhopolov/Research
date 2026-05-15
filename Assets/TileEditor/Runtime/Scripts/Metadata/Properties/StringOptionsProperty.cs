using System;
using System.Linq;
using UnityEngine;

namespace TileEditor
{
    public class StringOptionsProperty : StringProperty
    {
        [SerializeField] private string[] _options = new string[0];

        public string[] GetOptions() => _options;

        public int GetSelectedIndex()
        {
            var index = Array.IndexOf(_options, GetGenericValue());
            return index >= 0 ? index : 0;
        }

        public void SetOptions(string[] options)
        {
            _options = options ?? new string[0];
        }

        public override void SetValue(object value)
        {
            if (TrySetValueByIndex(value))
                return;

            var stringValue = Convert.ToString(value) ?? string.Empty;
            if (_options.Length > 0 && !_options.Contains(stringValue))
                stringValue = _options[0];

            base.SetValue(stringValue);
        }

        private bool TrySetValueByIndex(object value)
        {
            if (_options.Length == 0)
                return false;

            if (value == null || value is string || value is bool)
                return false;

            var index = Convert.ToInt32(value);
            if (index < 0)
                index = 0;
            else if (index >= _options.Length)
                index = _options.Length - 1;

            base.SetValue(_options[index]);
            return true;
        }
    }
}
