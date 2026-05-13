using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIShared.Tests.Editor
{
    public sealed class HudControllerTests
    {
        private GameObject _rootObject;
        private GameObject _prefab;
        private HudWidgetRegistryAsset _registry;
        private HudController _controller;
        private FakeHudPrefabLoader _loader;

        [SetUp]
        public void SetUp()
        {
            ControllerTestWidget.ResetCounters();

            _rootObject = new GameObject("HudRoot", typeof(RectTransform));
            var root = _rootObject.AddComponent<HudRoot>();

            _prefab = new GameObject("ControllerTestWidgetPrefab");
            _prefab.AddComponent<ControllerTestWidget>();

            _registry = ScriptableObject.CreateInstance<HudWidgetRegistryAsset>();
            _registry.SetDefinitionsForTests(new[]
            {
                new HudWidgetDefinition(
                    typeof(ControllerTestWidget).FullName,
                    "ControllerTestWidget",
                    HudLayer.World,
                    false)
            });

            _loader = new FakeHudPrefabLoader(_prefab);
            _controller = new HudController(root, _registry, _loader, null);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();

            if (_registry != null)
                Object.DestroyImmediate(_registry);

            if (_prefab != null)
                Object.DestroyImmediate(_prefab);

            if (_rootObject != null)
                Object.DestroyImmediate(_rootObject);
        }

        [Test]
        public void GetHudWidgetAsync_CreatesAndCachesWidget()
        {
            Assert.That(_controller.TryGetHudWidget<ControllerTestWidget>(out _), Is.False);

            var first = _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var second = _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var sync = _controller.GetHudWidget<ControllerTestWidget>();

            Assert.That(second, Is.SameAs(first));
            Assert.That(sync, Is.SameAs(first));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
            Assert.That(first.transform.parent.name, Is.EqualTo("WorldHudLayer"));
            Assert.That(ControllerTestWidget.CreatedCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseHudWidget_DestroysInstanceAndRemovesCache()
        {
            _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _controller.ReleaseHudWidget<ControllerTestWidget>();

            Assert.That(_controller.TryGetHudWidget<ControllerTestWidget>(out _), Is.False);
            Assert.That(ControllerTestWidget.BeforeReleasedCalls, Is.EqualTo(1));
        }

        [Test]
        public void HideHudWidget_DisablesInstanceAndKeepsCache()
        {
            var widget = _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _controller.HideHudWidget<ControllerTestWidget>();

            Assert.That(widget.gameObject.activeSelf, Is.False);
            Assert.That(_controller.TryGetHudWidget<ControllerTestWidget>(out var cachedWidget), Is.True);
            Assert.That(cachedWidget, Is.SameAs(widget));
            Assert.That(cachedWidget.gameObject.activeSelf, Is.False);
            Assert.That(ControllerTestWidget.BeforeReleasedCalls, Is.EqualTo(0));
        }

        [Test]
        public void GetHudWidgetAsync_EnablesHiddenCachedInstance()
        {
            var widget = _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            _controller.HideHudWidget<ControllerTestWidget>();

            var shownWidget = _controller
                .GetHudWidgetAsync<ControllerTestWidget>(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(shownWidget, Is.SameAs(widget));
            Assert.That(shownWidget.gameObject.activeSelf, Is.True);
            Assert.That(_loader.LoadCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateHudItemAsync_CreatesTransientItemUnderParent()
        {
            _prefab.AddComponent<ControllerTestItem>();
            var parent = new GameObject("ItemsRoot").transform;
            parent.SetParent(_rootObject.transform, false);

            var item = _controller
                .CreateHudItemAsync<ControllerTestItem>("ControllerTestItem", parent, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(item.transform.parent, Is.SameAs(parent));
            Assert.That(_loader.LoadCount, Is.EqualTo(1));

            _controller.ReleaseHudItem(item);

            Assert.That(item == null, Is.True);
        }

        public sealed class ControllerTestItem : MonoBehaviour
        {
        }

        private sealed class FakeHudPrefabLoader : IHudPrefabLoader
        {
            private readonly GameObject _prefab;

            public FakeHudPrefabLoader(GameObject prefab)
            {
                _prefab = prefab;
            }

            public int LoadCount { get; private set; }

            public UniTask<GameObject> LoadPrefabAsync(string addressableKey, CancellationToken cancellationToken)
            {
                LoadCount++;
                return UniTask.FromResult(_prefab);
            }

            public void ReleasePrefab(GameObject prefab)
            {
            }
        }

        public sealed class ControllerTestWidget : MonoBehaviour, IHudWidget, IHudWidgetLifecycle
        {
            public static int CreatedCalls { get; private set; }
            public static int BeforeReleasedCalls { get; private set; }

            public static void ResetCounters()
            {
                CreatedCalls = 0;
                BeforeReleasedCalls = 0;
            }

            public void OnCreatedByHudController()
            {
                CreatedCalls++;
            }

            public void OnBeforeReleased()
            {
                BeforeReleasedCalls++;
            }
        }
    }
}
