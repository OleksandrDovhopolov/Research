using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.Implementation.UI;
using UIShared;
using UISystem;
using UnityEngine;

namespace Inventory.Implementation
{
    public class InventoryWindowView : WindowView
    {
        [SerializeField] private UIListPool<InventoryView> _cardGroupsPool;
        
        [Space, Space, Header("Tabs")]
        [SerializeField] private GameObject _tabFocus;
        [SerializeField] private List<Transform> _tabs = new();
        
        [Space, Space, Header("Loading")]
        [SerializeField] private GameObject _loadingAnimationObject;
        
        [Space, Space, Header("InventoryContentWidget")]
        [SerializeField] private InventoryWidgetView _inventoryWidgetView;
        [SerializeField] private InventoryResourceWidgetView _inventoryResourceWidgetView;
        
        private CancellationTokenSource _windowLifetimeCts;
        
        private readonly Dictionary<string, InventoryView> _viewsByItemId = new();
        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, Task<Sprite>> _spriteLoadTasks = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();
        private readonly HashSet<string> _visibleItemIds = new();
        
        public event Action<int> TabClicked;
        public event Action BackgroundClicked;
        public event Action<InventoryView> OnOpenableViewClicked;

        protected override void Awake()
        {
            base.Awake();
            WidgetRegistry.Register<InventoryWidgetData>(_inventoryWidgetView);
            WidgetRegistry.Register<InventoryResourceWidgetData>(_inventoryResourceWidgetView);
        }

        public void OnTabClicked(int tabIndex)
        {
            if (!FocusTabVisual(tabIndex))
            {
                return;
            }
            
            TabClicked?.Invoke(tabIndex);
        }
        
        public void OnBackgroundClicked()
        {
            BackgroundClicked?.Invoke();
        }

        public void Render(List<InventoryItemUiModel> items)
        {
            if (items == null)
            {
                HideAllViews();
                return;
            }
            
            var token = GetWindowLifetimeToken();
            _visibleItemIds.Clear();
            var siblingIndex = 0;
            foreach (var item in items)
            {
                if (!_viewsByItemId.TryGetValue(item.ItemId, out var inventoryView))
                {
                    inventoryView = _cardGroupsPool.GetNext();
                    _viewsByItemId[item.ItemId] = inventoryView;
                }

                SubscribeInventoryView(inventoryView);
                inventoryView.SetData(item);
                inventoryView.transform.SetSiblingIndex(siblingIndex++);
                if (!inventoryView.gameObject.activeSelf)
                {
                    inventoryView.gameObject.SetActive(true);
                }
                
                _visibleItemIds.Add(item.ItemId);
                LoadSprite(inventoryView, item.ItemId, token).Forget();
            }

            foreach (var viewPair in _viewsByItemId.Where(viewPair => !_visibleItemIds.Contains(viewPair.Key)))
            {
                viewPair.Value.gameObject.SetActive(false);
            }
        }

        public bool FocusTabVisual(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count)
            {
                return false;
            }

            var tab = _tabs[tabIndex];
            if (tab == null || _tabFocus == null)
            {
                return false;
            }

            _tabFocus.transform.SetParent(tab, false);
            _tabFocus.transform.SetAsLastSibling();

            return true;
        }
        
        private async UniTask LoadSprite(InventoryView itemView, string spriteId, CancellationToken ct)
        {
            if (_spriteCache.TryGetValue(spriteId, out var cachedSprite))
            {
                if (itemView != null && itemView.ItemId == spriteId)
                {
                    itemView.SetSprite(cachedSprite);
                }

                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                _requestedSpriteAddresses.Add(spriteId);

                if (!_spriteLoadTasks.TryGetValue(spriteId, out var loadTask))
                {
                    loadTask = ProdAddressablesWrapper.LoadAsync<Sprite>(spriteId, ct);
                    _spriteLoadTasks[spriteId] = loadTask;
                }
                
                var sprite = await loadTask
                    .AsUniTask()
                    .AttachExternalCancellation(ct);
                
                _spriteCache[spriteId] = sprite;
                
                if (itemView == null || itemView.ItemId != spriteId)
                {
                    return;
                }
                
                itemView.SetSprite(sprite);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryWindowView] Failed to load sprite '{spriteId}'. {e}");
            }
            finally
            {
                _spriteLoadTasks.Remove(spriteId);
            }
        }

        internal CancellationToken GetWindowLifetimeToken()
        {
            if (_windowLifetimeCts != null)
            {
                return _windowLifetimeCts.Token;
            }
            
            _windowLifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            return _windowLifetimeCts.Token;
        }

        private void HideAllViews()
        {
            foreach (var inventoryView in _viewsByItemId.Values)
            {
                inventoryView.gameObject.SetActive(false);
            }
        }

        private void SubscribeInventoryView(InventoryView inventoryView)
        {
            if (inventoryView == null)
            {
                return;
            }

            inventoryView.OnInventoryViewClicked -= OnInventoryViewClickedHandler;
            inventoryView.OnInventoryViewClicked += OnInventoryViewClickedHandler;
        }

        private void UnsubscribeInventoryView(InventoryView inventoryView)
        {
            if (inventoryView == null)
            {
                return;
            }

            inventoryView.OnInventoryViewClicked -= OnInventoryViewClickedHandler;
        }

        private void OnInventoryViewClickedHandler(InventoryView inventoryView)
        {
            OnOpenableViewClicked?.Invoke(inventoryView);
        }
        
        public void ShowLoader(bool show)
        {
            _loadingAnimationObject.gameObject.SetActive(show);
        }
        
        public void Dispose()
        {
            _windowLifetimeCts?.Cancel();
            _windowLifetimeCts?.Dispose();
            _windowLifetimeCts = null;

            foreach (var inventoryView in _viewsByItemId.Values)
            {
                UnsubscribeInventoryView(inventoryView);
            }
            
            foreach (var spriteAddress in _requestedSpriteAddresses)
            {
                ProdAddressablesWrapper.Release(spriteAddress);
            }

            _requestedSpriteAddresses.Clear();
            _spriteCache.Clear();
            _spriteLoadTasks.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }
    }
}
