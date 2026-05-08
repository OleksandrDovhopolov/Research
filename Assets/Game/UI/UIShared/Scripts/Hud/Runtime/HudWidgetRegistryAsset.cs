using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    [CreateAssetMenu(fileName = "HudWidgetRegistry", menuName = "Game/UI/Hud Widget Registry")]
    public sealed class HudWidgetRegistryAsset : ScriptableObject
    {
        [SerializeField] private List<HudWidgetDefinition> _definitions = new();

        public IReadOnlyList<HudWidgetDefinition> Definitions => _definitions;

        public HudWidgetDefinition GetDefinition<TWidget>()
            where TWidget : Component, IHudWidget
        {
            return GetDefinition(typeof(TWidget));
        }

        public HudWidgetDefinition GetDefinition(Type widgetType)
        {
            if (widgetType == null)
                throw new ArgumentNullException(nameof(widgetType));

            if (!typeof(Component).IsAssignableFrom(widgetType) || !typeof(IHudWidget).IsAssignableFrom(widgetType))
                throw new InvalidOperationException($"Type '{widgetType.FullName}' must be a Unity Component implementing {nameof(IHudWidget)}.");

            var widgetTypeName = widgetType.FullName;
            HudWidgetDefinition result = null;
            var matches = 0;

            foreach (var definition in _definitions)
            {
                if (definition == null)
                    continue;

                if (!string.Equals(definition.WidgetTypeName, widgetTypeName, StringComparison.Ordinal))
                    continue;

                result = definition;
                matches++;
            }

            if (matches == 0)
                throw new InvalidOperationException($"HUD widget definition for '{widgetTypeName}' was not found.");

            if (matches > 1)
                throw new InvalidOperationException($"HUD widget definition for '{widgetTypeName}' is duplicated.");

            if (string.IsNullOrWhiteSpace(result.AddressableKey))
                throw new InvalidOperationException($"HUD widget definition for '{widgetTypeName}' has empty addressable key.");

            return result;
        }

        public Type ResolveWidgetType(HudWidgetDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var typeName = definition.WidgetTypeName;
            if (string.IsNullOrWhiteSpace(typeName))
                throw new InvalidOperationException("HUD widget definition has empty type name.");

            var type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        break;
                }
            }

            if (type == null)
                throw new InvalidOperationException($"HUD widget type '{typeName}' was not found.");

            if (!typeof(Component).IsAssignableFrom(type) || !typeof(IHudWidget).IsAssignableFrom(type))
                throw new InvalidOperationException($"HUD widget type '{typeName}' must be a Unity Component implementing {nameof(IHudWidget)}.");

            return type;
        }

#if UNITY_EDITOR
        public void SetDefinitionsForTests(IEnumerable<HudWidgetDefinition> definitions)
        {
            _definitions = definitions != null
                ? new List<HudWidgetDefinition>(definitions)
                : new List<HudWidgetDefinition>();
        }
#endif
    }
}
