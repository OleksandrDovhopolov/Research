using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TileEditor
{
    public class ColorProperty : BaseProperty
    {
        public UnityEvent<Color> onValueChangeEvent = new UnityEvent<Color>();

        private string _serializedColorValue;
        public Color Color { get; private set; }

        public override object GetDefaultValue() => "1;1;1;1";

        public override object GetValue() => _serializedColorValue;

        public override void SetValue(object value)
        {
            _serializedColorValue = (string) value;
            var colorChannels = _serializedColorValue.Split(';').Select(float.Parse).ToArray();
            Color = new Color(colorChannels[0], colorChannels[1], colorChannels[2], colorChannels[3]);
            onValueChangeEvent?.Invoke(Color);
        }
    }
}
