using System;
using System.Collections.Generic;

namespace UIShared
{
    public static class WidgetRegistry
    {
        private static readonly Dictionary<Type, IContentWidgetView> Prefabs = new();

        public static void Register<TData>(IContentWidgetView prefab) where TData : ContentWidgetDataBase
        {
            Prefabs[typeof(TData)] = prefab;
        }

        public static IContentWidgetView GetPrefab(Type dataType)
        {
            Prefabs.TryGetValue(dataType, out var prefab);
            return prefab;
        }
    }
}