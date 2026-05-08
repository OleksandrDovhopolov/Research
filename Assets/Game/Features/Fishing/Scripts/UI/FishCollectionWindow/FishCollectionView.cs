using System;
using System.Collections.Generic;
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
        [SerializeField] private UIListPool<FishCollectionItemView> _entriesPool;
        [SerializeField] private ScrollRect _scrollRect;

        private CancellationTokenSource _windowLifetimeCts;
        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, Task<Sprite>> _spriteLoadTasks = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();

        public void Render(IReadOnlyList<FishCollectionEntryViewData> entries)
        {
            _entriesPool?.DisableAll();

            if (entries == null || entries.Count == 0 || _entriesPool == null)
                return;

            var token = GetWindowLifetimeToken();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var view = _entriesPool.GetNext();
                view.transform.SetSiblingIndex(i);
                view.SetData(entry);
                LoadSpriteAsync(view, entry.SpriteAddress, token).Forget();
            }

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
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
            _windowLifetimeCts?.Cancel();
            _windowLifetimeCts?.Dispose();
            _windowLifetimeCts = null;

            _entriesPool?.DisableAll();

            foreach (var spriteAddress in _requestedSpriteAddresses)
                ProdAddressablesWrapper.Release(spriteAddress);

            _requestedSpriteAddresses.Clear();
            _spriteCache.Clear();
            _spriteLoadTasks.Clear();
        }

        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }

        private async UniTask LoadSpriteAsync(FishCollectionItemView itemView, string spriteAddress, CancellationToken ct)
        {
            if (itemView == null || string.IsNullOrWhiteSpace(spriteAddress))
                return;

            if (_spriteCache.TryGetValue(spriteAddress, out var cachedSprite))
            {
                if (itemView.SpriteAddress == spriteAddress)
                    itemView.SetSprite(cachedSprite);
                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                _requestedSpriteAddresses.Add(spriteAddress);

                if (!_spriteLoadTasks.TryGetValue(spriteAddress, out var loadTask))
                {
                    loadTask = ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
                    _spriteLoadTasks[spriteAddress] = loadTask;
                }

                var sprite = await loadTask.AsUniTask().AttachExternalCancellation(ct);
                if (sprite == null)
                {
                    Debug.LogWarning($"[FishCollectionView] Sprite not found for id '{spriteAddress}'.");
                    return;
                }

                _spriteCache[spriteAddress] = sprite;

                if (itemView != null && itemView.SpriteAddress == spriteAddress)
                    itemView.SetSprite(sprite);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishCollectionView] Failed to load sprite '{spriteAddress}'. {exception}");
            }
            finally
            {
                _spriteLoadTasks.Remove(spriteAddress);
            }
        }
    }
}
