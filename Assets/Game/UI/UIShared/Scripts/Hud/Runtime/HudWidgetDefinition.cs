using System;
using UnityEngine;

namespace UIShared
{
    [Serializable]
    public sealed class HudWidgetDefinition
    {
        [SerializeField] private string _widgetTypeName;
        [SerializeField] private string _addressableKey;
        [SerializeField] private HudLayer _layer;
        [SerializeField] private bool _createOnInitialize;

        public HudWidgetDefinition(
            string widgetTypeName,
            string addressableKey,
            HudLayer layer,
            bool createOnInitialize)
        {
            _widgetTypeName = widgetTypeName;
            _addressableKey = addressableKey;
            _layer = layer;
            _createOnInitialize = createOnInitialize;
        }

        public string WidgetTypeName => _widgetTypeName;
        public string AddressableKey => _addressableKey;
        public HudLayer Layer => _layer;
        public bool CreateOnInitialize => _createOnInitialize;
    }
}
