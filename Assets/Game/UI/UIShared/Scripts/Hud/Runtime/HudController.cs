using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace UIShared
{
    public sealed class HudController : IHudController, IDisposable
    {
        private readonly HudRoot _hudRoot;
        private readonly HudWidgetRegistryAsset _registry;
        private readonly IHudPrefabLoader _prefabLoader;
        private readonly IObjectResolver _resolver;

        private readonly Dictionary<Type, Component> _createdWidgets = new();
        private readonly Dictionary<Type, GameObject> _loadedPrefabs = new();
        private readonly Dictionary<Type, Task<Component>> _creationTasks = new();
        private readonly Dictionary<string, GameObject> _loadedItemPrefabs = new(StringComparer.Ordinal);

        private bool _isDisposed;

        public HudController(
            HudRoot hudRoot,
            HudWidgetRegistryAsset registry,
            IHudPrefabLoader prefabLoader,
            IObjectResolver resolver)
        {
            _hudRoot = hudRoot != null
                ? hudRoot
                : throw new ArgumentNullException(nameof(hudRoot));
            _registry = registry != null
                ? registry
                : throw new ArgumentNullException(nameof(registry));
            _prefabLoader = prefabLoader != null
                ? prefabLoader
                : throw new ArgumentNullException(nameof(prefabLoader));
            _resolver = resolver;
        }

        public async UniTask CreateInitialWidgetsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            foreach (var definition in _registry.Definitions)
            {
                if (definition == null || !definition.CreateOnInitialize)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                var widgetType = _registry.ResolveWidgetType(definition);
                await GetHudWidgetAsync(widgetType, cancellationToken);
            }
        }

        public async UniTask<TWidget> GetHudWidgetAsync<TWidget>(CancellationToken cancellationToken)
            where TWidget : Component, IHudWidget
        {
            var widget = await GetHudWidgetAsync(typeof(TWidget), cancellationToken);
            return (TWidget)widget;
        }

        public TWidget GetHudWidget<TWidget>()
            where TWidget : Component, IHudWidget
        {
            ThrowIfDisposed();

            var widgetType = typeof(TWidget);
            if (_createdWidgets.TryGetValue(widgetType, out var widget))
            {
                if (widget != null)
                {
                    widget.gameObject.SetActive(true);
                    return (TWidget)widget;
                }

                _createdWidgets.Remove(widgetType);
            }

            throw new InvalidOperationException(
                $"HUD widget '{widgetType.Name}' is not created. Use {nameof(GetHudWidgetAsync)} first or mark it as create-on-initialize.");
        }

        public async UniTask<TItem> CreateHudItemAsync<TItem>(
            string addressableKey,
            Transform parent,
            CancellationToken cancellationToken)
            where TItem : Component
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(addressableKey))
                throw new ArgumentException("HUD item addressable key must be assigned.", nameof(addressableKey));

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var prefab = await LoadItemPrefabAsync(addressableKey, cancellationToken);
            var instance = _resolver != null
                ? _resolver.Instantiate(prefab, parent)
                : Object.Instantiate(prefab, parent, false);

            instance.transform.SetParent(parent, false);

            var item = instance.GetComponent<TItem>();
            if (item != null)
                return item;

            DestroyInstance(instance);
            throw new InvalidOperationException(
                $"HUD item prefab '{addressableKey}' does not contain component '{typeof(TItem).Name}'.");
        }

        public bool TryGetHudWidget<TWidget>(out TWidget widget)
            where TWidget : Component, IHudWidget
        {
            ThrowIfDisposed();

            var widgetType = typeof(TWidget);
            if (_createdWidgets.TryGetValue(widgetType, out var existingWidget))
            {
                if (existingWidget != null)
                {
                    widget = (TWidget)existingWidget;
                    return true;
                }

                _createdWidgets.Remove(widgetType);
            }

            widget = null;
            return false;
        }

        public void HideHudWidget<TWidget>()
            where TWidget : Component, IHudWidget
        {
            ThrowIfDisposed();

            var widgetType = typeof(TWidget);
            ValidateWidgetType(widgetType);

            if (!_createdWidgets.TryGetValue(widgetType, out var widget))
                return;

            if (widget == null)
            {
                _createdWidgets.Remove(widgetType);
                return;
            }

            widget.gameObject.SetActive(false);
        }

        public void ReleaseHudWidget<TWidget>()
            where TWidget : Component, IHudWidget
        {
            ReleaseHudWidget(typeof(TWidget));
        }

        public void ReleaseHudItem(Component item)
        {
            if (item == null)
                return;

            DestroyInstance(item.gameObject);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var widget in _createdWidgets.Values)
                ReleaseWidgetInstance(widget);

            _createdWidgets.Clear();
            _creationTasks.Clear();

            foreach (var prefab in _loadedPrefabs.Values)
                _prefabLoader.ReleasePrefab(prefab);

            _loadedPrefabs.Clear();

            foreach (var prefab in _loadedItemPrefabs.Values)
                _prefabLoader.ReleasePrefab(prefab);

            _loadedItemPrefabs.Clear();
        }

        private async UniTask<Component> GetHudWidgetAsync(Type widgetType, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ValidateWidgetType(widgetType);

            if (_createdWidgets.TryGetValue(widgetType, out var existingWidget))
            {
                if (existingWidget != null)
                {
                    existingWidget.gameObject.SetActive(true);
                    return existingWidget;
                }

                _createdWidgets.Remove(widgetType);
            }

            if (_creationTasks.TryGetValue(widgetType, out var existingTask))
                return await AwaitWithCancellation(existingTask, cancellationToken);

            var creationTask = CreateWidgetAsync(widgetType, cancellationToken);
            _creationTasks.Add(widgetType, creationTask);

            try
            {
                return await creationTask;
            }
            finally
            {
                if (_creationTasks.TryGetValue(widgetType, out var storedTask) && storedTask == creationTask)
                    _creationTasks.Remove(widgetType);
            }
        }

        private async Task<Component> CreateWidgetAsync(Type widgetType, CancellationToken cancellationToken)
        {
            var definition = _registry.GetDefinition(widgetType);
            var prefab = await LoadPrefabAsync(widgetType, definition, cancellationToken);
            var parent = _hudRoot.GetLayerRoot(definition.Layer);

            var instance = _resolver != null
                ? _resolver.Instantiate(prefab, parent)
                : Object.Instantiate(prefab, parent, false);

            instance.name = widgetType.Name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var widget = instance.GetComponent(widgetType) as Component;
            if (widget == null)
            {
                DestroyInstance(instance);
                throw new InvalidOperationException(
                    $"HUD prefab '{definition.AddressableKey}' does not contain component '{widgetType.Name}'.");
            }

            _createdWidgets.Add(widgetType, widget);

            if (widget is IHudWidgetLifecycle lifecycle)
                lifecycle.OnCreatedByHudController();

            return widget;
        }

        private async Task<GameObject> LoadPrefabAsync(
            Type widgetType,
            HudWidgetDefinition definition,
            CancellationToken cancellationToken)
        {
            if (_loadedPrefabs.TryGetValue(widgetType, out var loadedPrefab))
            {
                if (loadedPrefab != null)
                    return loadedPrefab;

                _loadedPrefabs.Remove(widgetType);
            }

            var prefab = await _prefabLoader.LoadPrefabAsync(definition.AddressableKey, cancellationToken);
            if (prefab == null)
                throw new InvalidOperationException($"Loaded null HUD prefab from address '{definition.AddressableKey}'.");

            _loadedPrefabs.Add(widgetType, prefab);
            return prefab;
        }

        private async Task<GameObject> LoadItemPrefabAsync(
            string addressableKey,
            CancellationToken cancellationToken)
        {
            if (_loadedItemPrefabs.TryGetValue(addressableKey, out var loadedPrefab))
            {
                if (loadedPrefab != null)
                    return loadedPrefab;

                _loadedItemPrefabs.Remove(addressableKey);
            }

            var prefab = await _prefabLoader.LoadPrefabAsync(addressableKey, cancellationToken);
            if (prefab == null)
                throw new InvalidOperationException($"Loaded null HUD item prefab from address '{addressableKey}'.");

            _loadedItemPrefabs.Add(addressableKey, prefab);
            return prefab;
        }

        private void ReleaseHudWidget(Type widgetType)
        {
            ThrowIfDisposed();
            ValidateWidgetType(widgetType);

            if (!_createdWidgets.Remove(widgetType, out var widget))
                return;

            ReleaseWidgetInstance(widget);
        }

        private static void ReleaseWidgetInstance(Component widget)
        {
            if (widget == null)
                return;

            if (widget is IHudWidgetLifecycle lifecycle)
                lifecycle.OnBeforeReleased();

            DestroyInstance(widget.gameObject);
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (instance == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HudController));
        }

        private static void ValidateWidgetType(Type widgetType)
        {
            if (widgetType == null)
                throw new ArgumentNullException(nameof(widgetType));

            if (!typeof(Component).IsAssignableFrom(widgetType) || !typeof(IHudWidget).IsAssignableFrom(widgetType))
                throw new InvalidOperationException($"Type '{widgetType.FullName}' must be a Unity Component implementing {nameof(IHudWidget)}.");
        }

        private static async Task<T> AwaitWithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
                return await task;

            var cancellationCompletionSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                       static state => ((TaskCompletionSource<bool>)state).TrySetResult(true),
                       cancellationCompletionSource))
            {
                var completedTask = await Task.WhenAny(task, cancellationCompletionSource.Task);
                if (completedTask != task)
                    throw new OperationCanceledException(cancellationToken);
            }

            return await task;
        }
    }
}
