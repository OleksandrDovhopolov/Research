using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;
using UIShared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishCollectionViewTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();
        private Func<string, CancellationToken, Task<Sprite>> _originalSpriteLoader;
        private Action<string> _originalSpriteReleaser;
        private TimeSpan _originalLoadTimeout;

        [SetUp]
        public void SetUp()
        {
            _originalSpriteLoader = GetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader");
            _originalSpriteReleaser = GetStaticField<Action<string>>("s_spriteReleaser");
            _originalLoadTimeout = GetStaticField<TimeSpan>("s_loadTimeout");
        }

        [TearDown]
        public void TearDown()
        {
            SetStaticField("s_spriteLoader", _originalSpriteLoader);
            SetStaticField("s_spriteReleaser", _originalSpriteReleaser);
            SetStaticField("s_loadTimeout", _originalLoadTimeout);

            LogAssert.NoUnexpectedReceived();

            for (var i = _objectsToCleanup.Count - 1; i >= 0; i--)
            {
                if (_objectsToCleanup[i] != null)
                    UnityEngine.Object.DestroyImmediate(_objectsToCleanup[i]);
            }

            _objectsToCleanup.Clear();
        }

        [UnityTest]
        public System.Collections.IEnumerator Render_ShowsContentAfterSuccessfulLoad_AndReusesCachedSprites()
        {
            var loadCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var spriteA = CreateSprite();
            var spriteB = CreateSprite();
            var sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal)
            {
                ["fish_a"] = spriteA,
                ["fish_b"] = spriteB,
                ["lure_a"] = spriteA,
                ["lure_b"] = spriteB
            };

            SetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader", async (address, _) =>
            {
                loadCounts[address] = loadCounts.TryGetValue(address, out var count) ? count + 1 : 1;
                await Task.Yield();
                return sprites[address];
            });

            var (view, contentContainer, loadingContainer, _) = CreateView();
            var entries = new[]
            {
                CreateEntry("fish_a", "lure_a"),
                CreateEntry("fish_b", "lure_b")
            };

            view.Render(entries);

            Assert.That(loadingContainer.activeSelf, Is.True);
            Assert.That(contentContainer.activeSelf, Is.False);

            yield return null;

            Assert.That(loadingContainer.activeSelf, Is.False);
            Assert.That(contentContainer.activeSelf, Is.True);
            Assert.That(loadCounts["fish_a"], Is.EqualTo(1));
            Assert.That(loadCounts["fish_b"], Is.EqualTo(1));
            Assert.That(loadCounts["lure_a"], Is.EqualTo(1));
            Assert.That(loadCounts["lure_b"], Is.EqualTo(1));

            view.Render(entries);

            Assert.That(loadingContainer.activeSelf, Is.False);
            Assert.That(contentContainer.activeSelf, Is.True);
            Assert.That(loadCounts["fish_a"], Is.EqualTo(1));
            Assert.That(loadCounts["fish_b"], Is.EqualTo(1));
            Assert.That(loadCounts["lure_a"], Is.EqualTo(1));
            Assert.That(loadCounts["lure_b"], Is.EqualTo(1));
        }

        [UnityTest]
        public System.Collections.IEnumerator Render_LoadsDuplicateSpriteAddressOnlyOnce()
        {
            var loadCount = 0;
            var sprite = CreateSprite();

            SetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader", async (_, _) =>
            {
                loadCount++;
                await Task.Yield();
                return sprite;
            });

            var (view, contentContainer, loadingContainer, _) = CreateView();
            var entries = new[]
            {
                CreateEntry("shared_fish", "shared_lure"),
                CreateEntry("shared_fish", "shared_lure")
            };

            view.Render(entries);
            yield return null;

            Assert.That(loadCount, Is.EqualTo(2));
            Assert.That(loadingContainer.activeSelf, Is.False);
            Assert.That(contentContainer.activeSelf, Is.True);
        }

        [UnityTest]
        public System.Collections.IEnumerator Render_WhenSpriteLoadFails_HidesLoader_KeepsContentHidden_AndUnblocksClose()
        {
            SetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader", async (address, _) =>
            {
                await Task.Yield();
                throw new InvalidOperationException($"Failure for {address}");
            });

            var (view, contentContainer, loadingContainer, _) = CreateView();
            LogAssert.Expect(LogType.Error, new Regex(@"\[FishCollectionView\] Failed to load fish collection sprites\."));

            view.Render(new[] { CreateEntry("broken_fish") });

            Assert.That(loadingContainer.activeSelf, Is.True);
            Assert.That(contentContainer.activeSelf, Is.False);

            yield return null;

            Assert.That(loadingContainer.activeSelf, Is.False);
            Assert.That(contentContainer.activeSelf, Is.False);
            Assert.That(GetIsLoading(view), Is.False);
        }

        [UnityTest]
        public System.Collections.IEnumerator Render_WhenLoadTimesOut_HidesLoader_KeepsContentHidden_AndUnblocksClose()
        {
            var pendingTaskSource = new TaskCompletionSource<Sprite>();
            SetStaticField("s_loadTimeout", TimeSpan.FromSeconds(0.05));
            SetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader", (_, _) => pendingTaskSource.Task);

            var (view, contentContainer, loadingContainer, _) = CreateView();
            LogAssert.Expect(LogType.Error, new Regex(@"\[FishCollectionView\] Timed out after 0\.05 seconds while loading fish collection sprites\."));

            view.Render(new[] { CreateEntry("slow_fish") });

            Assert.That(loadingContainer.activeSelf, Is.True);
            Assert.That(contentContainer.activeSelf, Is.False);

            var waitStartedAt = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - waitStartedAt < 0.2f)
                yield return null;

            Assert.That(loadingContainer.activeSelf, Is.False);
            Assert.That(contentContainer.activeSelf, Is.False);
            Assert.That(GetIsLoading(view), Is.False);
        }

        [UnityTest]
        public System.Collections.IEnumerator Dispose_ReleasesRequestedSpritesOnlyOnDestroy()
        {
            var loadCount = 0;
            var releaseCount = 0;
            var sprite = CreateSprite();

            SetStaticField<Func<string, CancellationToken, Task<Sprite>>>("s_spriteLoader", (_, _) =>
            {
                loadCount++;
                return Task.FromResult(sprite);
            });
            SetStaticField<Action<string>>("s_spriteReleaser", address =>
            {
                if (address == "fish_release" || address == "lure_release")
                    releaseCount++;
            });

            var (view, contentContainer, _, _) = CreateView();
            var entries = new[] { CreateEntry("fish_release", "lure_release") };

            view.Render(entries);
            yield return null;

            Assert.That(contentContainer.activeSelf, Is.True);
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(releaseCount, Is.EqualTo(0));

            view.Render(entries);

            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(releaseCount, Is.EqualTo(0));

            UnityEngine.Object.DestroyImmediate(view.gameObject);

            Assert.That(releaseCount, Is.EqualTo(2));
        }

        private (FishCollectionView View, GameObject ContentContainer, GameObject LoadingContainer, UIListPool<FishCollectionItemView> Pool) CreateView()
        {
            var root = new GameObject("FishCollectionViewRoot", typeof(RectTransform));
            var contentContainer = new GameObject("ContentContainer", typeof(RectTransform));
            var loadingContainer = new GameObject("LoadingContainer", typeof(RectTransform));
            var poolParent = new GameObject("PoolParent", typeof(RectTransform));
            var prefab = new GameObject("FishCollectionItemPrefab");

            _objectsToCleanup.Add(root);
            _objectsToCleanup.Add(prefab);

            contentContainer.transform.SetParent(root.transform, false);
            loadingContainer.transform.SetParent(root.transform, false);
            poolParent.transform.SetParent(contentContainer.transform, false);

            prefab.AddComponent<RectTransform>();
            prefab.AddComponent<FishCollectionItemView>();
            prefab.SetActive(false);

            var view = root.AddComponent<FishCollectionView>();
            var pool = new UIListPool<FishCollectionItemView>(prefab, poolParent.transform, 0);

            SetInstanceField(view, "_entriesPool", pool);
            SetInstanceField(view, "_contentContainer", contentContainer);
            SetInstanceField(view, "_loadingContainer", loadingContainer);

            return (view, contentContainer, loadingContainer, pool);
        }

        private FishCollectionEntryViewData CreateEntry(string spriteAddress, params string[] lureSpriteAddresses)
        {
            var lures = lureSpriteAddresses == null
                ? Array.Empty<FishCollectionLureViewData>()
                : Array.ConvertAll(lureSpriteAddresses, lureSpriteAddress =>
                    new FishCollectionLureViewData(lureSpriteAddress, lureSpriteAddress, lureSpriteAddress));

            return new FishCollectionEntryViewData(
                fishId: spriteAddress,
                spriteAddress: spriteAddress,
                displayName: spriteAddress,
                waterBodyTypesText: string.Empty,
                behaviorType: string.Empty,
                itemType: "fish",
                minWeight: 0f,
                maxWeight: 0f,
                bestCaughtWeight: 0f,
                isDiscovered: false,
                lures: lures,
                progress: null);
        }

        private Sprite CreateSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _objectsToCleanup.Add(texture);

            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            _objectsToCleanup.Add(sprite);
            return sprite;
        }

        private static bool GetIsLoading(FishCollectionView view)
        {
            var property = typeof(FishCollectionView).GetProperty("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            return (bool)property.GetValue(view);
        }

        private static T GetStaticField<T>(string fieldName)
        {
            var field = typeof(FishCollectionView).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Static field '{fieldName}' was not found.");
            return (T)field.GetValue(null);
        }

        private static void SetStaticField<T>(string fieldName, T value)
        {
            var field = typeof(FishCollectionView).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Static field '{fieldName}' was not found.");
            field.SetValue(null, value);
        }

        private static void SetInstanceField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Instance field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }
    }
}
