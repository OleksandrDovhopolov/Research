using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class FishCollectionView : WindowView
    {
        private static readonly TimeSpan DefaultLoadTimeout = TimeSpan.FromSeconds(10);
        private static Func<string, CancellationToken, Task<Sprite>> s_spriteLoader = ProdAddressablesWrapper.LoadAsync<Sprite>;
        private static Action<string> s_spriteReleaser = ProdAddressablesWrapper.Release;
        private static TimeSpan s_loadTimeout = DefaultLoadTimeout;

        [SerializeField] private UIListPool<FishCollectionItemView> _entriesPool;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GameObject _contentContainer;
        [SerializeField] private GameObject _loadingContainer;

        private CancellationTokenSource _windowLifetimeCts;
        private CancellationTokenSource _activeLoadCts;
        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, SpriteLoadOperation> _spriteLoadTasks = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();
        private int _renderSessionId;
        private bool _isLoading;

        internal bool IsLoading => _isLoading;

        public void Render(IReadOnlyList<FishCollectionEntryViewData> entries)
        {
            CancelActiveLoad();
            _entriesPool?.DisableAll();
            SetLoadingState(false);
            SetContentVisible(false);

            if (entries == null || entries.Count == 0 || _entriesPool == null)
                return;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var view = _entriesPool.GetNext();
                view.transform.SetSiblingIndex(i);
                view.SetData(entry);
            }

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;

            ApplySpritesToVisibleItems();

            var spriteAddressesToLoad = entries
                .SelectMany(GetSpriteAddresses)
                .Where(spriteAddress => !string.IsNullOrWhiteSpace(spriteAddress))
                .Distinct(StringComparer.Ordinal)
                .Where(spriteAddress => !_spriteCache.ContainsKey(spriteAddress))
                .ToArray();

            if (spriteAddressesToLoad.Length == 0)
            {
                SetContentVisible(true);
                return;
            }

            unchecked
            {
                _renderSessionId++;
            }

            LoadSpritesBatchAsync(spriteAddressesToLoad, _renderSessionId).Forget();
        }

        public CancellationToken GetWindowLifetimeToken()
        {
            if (_windowLifetimeCts != null)
                return _windowLifetimeCts.Token;

            _windowLifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            return _windowLifetimeCts.Token;
        }

        public void Dispose()
        {
            CancelActiveLoad();

            _windowLifetimeCts?.Cancel();
            _windowLifetimeCts?.Dispose();
            _windowLifetimeCts = null;

            _entriesPool?.DisableAll();

            foreach (var spriteAddress in _requestedSpriteAddresses)
                s_spriteReleaser(spriteAddress);

            _requestedSpriteAddresses.Clear();
            _spriteCache.Clear();
            _spriteLoadTasks.Clear();
            SetLoadingState(false);
            SetContentVisible(false);
        }

        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }

        private async UniTaskVoid LoadSpritesBatchAsync(IReadOnlyList<string> spriteAddresses, int renderSessionId)
        {
            CancelActiveLoad();

            var windowLifetimeToken = GetWindowLifetimeToken();
            var renderCts = new CancellationTokenSource();
            var timeoutCts = new CancellationTokenSource();
            timeoutCts.CancelAfter(s_loadTimeout);
            var loadCts = CancellationTokenSource.CreateLinkedTokenSource(windowLifetimeToken, renderCts.Token, timeoutCts.Token);
            _activeLoadCts = renderCts;
            SetLoadingState(true);

            try
            {
                var loadTasks = new UniTask[spriteAddresses.Count];
                for (var i = 0; i < spriteAddresses.Count; i++)
                {
                    loadTasks[i] = EnsureSpriteLoadedAsync(spriteAddresses[i], loadCts);
                }

                await UniTask.WhenAll(loadTasks);

                if (!IsCurrentRenderSession(renderSessionId))
                    return;

                ApplySpritesToVisibleItems();
                SetLoadingState(false);
                SetContentVisible(true);
            }
            catch (OperationCanceledException)
            {
                if (!IsCurrentRenderSession(renderSessionId) || windowLifetimeToken.IsCancellationRequested || renderCts.IsCancellationRequested)
                    return;

                SetLoadingState(false);
                SetContentVisible(false);
                Debug.LogError($"[FishCollectionView] Timed out after {s_loadTimeout.TotalSeconds:0.##} seconds while loading fish collection sprites.");
            }
            catch (Exception exception)
            {
                if (!IsCurrentRenderSession(renderSessionId) || windowLifetimeToken.IsCancellationRequested || renderCts.IsCancellationRequested)
                    return;

                SetLoadingState(false);
                SetContentVisible(false);
                Debug.LogError($"[FishCollectionView] Failed to load fish collection sprites. {exception}");
            }
            finally
            {
                loadCts.Dispose();
                timeoutCts.Dispose();

                if (ReferenceEquals(_activeLoadCts, renderCts))
                {
                    _activeLoadCts = null;
                }

                renderCts.Dispose();
            }
        }

        private async UniTask EnsureSpriteLoadedAsync(string spriteAddress, CancellationTokenSource loadCts)
        {
            if (string.IsNullOrWhiteSpace(spriteAddress) || _spriteCache.ContainsKey(spriteAddress))
                return;

            SpriteLoadOperation loadOperation;
            if (_spriteLoadTasks.TryGetValue(spriteAddress, out loadOperation))
            {
                if (loadOperation.OwnerCts == null ||
                    loadOperation.OwnerCts.IsCancellationRequested ||
                    loadOperation.Task.IsCanceled ||
                    loadOperation.Task.IsFaulted)
                {
                    _spriteLoadTasks.Remove(spriteAddress);
                    loadOperation = null;
                }
            }

            if (loadOperation == null)
            {
                _requestedSpriteAddresses.Add(spriteAddress);
                loadOperation = new SpriteLoadOperation(LoadSpriteCoreAsync(spriteAddress, loadCts.Token), loadCts);
                _spriteLoadTasks[spriteAddress] = loadOperation;
            }

            try
            {
                var sprite = await loadOperation.Task.AsUniTask().AttachExternalCancellation(loadCts.Token);
                if (sprite == null)
                    throw new InvalidOperationException($"Loaded null sprite for address '{spriteAddress}'.");

                _spriteCache[spriteAddress] = sprite;
            }
            finally
            {
                if (_spriteLoadTasks.TryGetValue(spriteAddress, out var currentOperation) &&
                    ReferenceEquals(currentOperation, loadOperation) &&
                    loadOperation.Task.IsCompleted)
                {
                    _spriteLoadTasks.Remove(spriteAddress);
                }
            }
        }

        private void ApplySpritesToVisibleItems()
        {
            if (_entriesPool == null)
                return;

            foreach (var itemView in _entriesPool.ActiveElements())
            {
                if (itemView == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(itemView.SpriteAddress) &&
                    _spriteCache.TryGetValue(itemView.SpriteAddress, out var fishSprite))
                {
                    itemView.SetSprite(fishSprite);
                }

                var lureSpriteAddresses = itemView.LureSpriteAddresses;
                if (lureSpriteAddresses == null)
                    continue;

                for (var i = 0; i < lureSpriteAddresses.Count; i++)
                {
                    var lureSpriteAddress = lureSpriteAddresses[i];
                    if (string.IsNullOrWhiteSpace(lureSpriteAddress))
                        continue;

                    if (_spriteCache.TryGetValue(lureSpriteAddress, out var lureSprite))
                        itemView.SetLureSprite(lureSpriteAddress, lureSprite);
                }
            }
        }

        private static IEnumerable<string> GetSpriteAddresses(FishCollectionEntryViewData entry)
        {
            if (entry == null)
                yield break;

            if (!string.IsNullOrWhiteSpace(entry.SpriteAddress))
                yield return entry.SpriteAddress;

            if (entry.Lures == null)
                yield break;

            for (var i = 0; i < entry.Lures.Count; i++)
            {
                var lure = entry.Lures[i];
                if (lure != null && !string.IsNullOrWhiteSpace(lure.SpriteAddress))
                    yield return lure.SpriteAddress;
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            if (_loadingContainer != null)
                _loadingContainer.SetActive(isLoading);
        }

        private void SetContentVisible(bool isVisible)
        {
            if (_contentContainer != null)
                _contentContainer.SetActive(isVisible);
        }

        private bool IsCurrentRenderSession(int renderSessionId)
        {
            return renderSessionId == _renderSessionId;
        }

        private void CancelActiveLoad()
        {
            if (_activeLoadCts == null)
                return;

            _activeLoadCts.Cancel();
            _activeLoadCts.Dispose();
            _activeLoadCts = null;
        }

        private static Task<Sprite> LoadSpriteCoreAsync(string spriteAddress, CancellationToken ct)
        {
            return s_spriteLoader(spriteAddress, ct);
        }

        private sealed class SpriteLoadOperation
        {
            public SpriteLoadOperation(Task<Sprite> task, CancellationTokenSource ownerCts)
            {
                Task = task;
                OwnerCts = ownerCts;
            }

            public Task<Sprite> Task { get; }
            public CancellationTokenSource OwnerCts { get; }
        }
    }
}
