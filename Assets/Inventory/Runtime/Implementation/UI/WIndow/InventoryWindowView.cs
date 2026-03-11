using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
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

        
        private CancellationTokenSource _loadSpritesCts;
        private InventoryItemCategory _currentCategory = InventoryItemCategory.Regular;
        private readonly Dictionary<InventoryItemCategory, List<InventoryItemUiModel>> _itemsByCategory = new();

        // Temporary hardcoded tab-category mapping by tab index.
        // Update this mapping later when categories are configured from data/editor.
        private static readonly List<InventoryItemCategory> TabCategories = new()
        {
            InventoryItemCategory.Regular,  // Tab 0
            InventoryItemCategory.Regular,  // Tab 1
            InventoryItemCategory.Regular,  // Tab 2
            InventoryItemCategory.Regular,  // Tab 3
            InventoryItemCategory.CardPack, // Tab 4
        };

        private void Start()
        {
            if (_tabs.Count > 0)
            {
                FocusTab(0, false);
            }
        }

        public void OnTabClicked(Transform tab)
        {
            if (tab == null)
            {
                return;
            }

            var tabIndex = _tabs.IndexOf(tab);
            if (tabIndex < 0)
            {
                return;
            }

            FocusTab(tabIndex, true);
        }

        public void FocusTab(int tabIndex, bool shouldRender = true)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count)
            {
                return;
            }

            var tab = _tabs[tabIndex];
            if (tab == null || _tabFocus == null)
            {
                return;
            }

            _tabFocus.transform.SetParent(tab, false);
            _tabFocus.transform.SetAsLastSibling();
            _currentCategory = ResolveCategory(tabIndex);

            if (shouldRender)
            {
                RenderCurrentCategory();
            }
        }

        private static InventoryItemCategory ResolveCategory(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= TabCategories.Count)
            {
                return InventoryItemCategory.Regular;
            }

            return TabCategories[tabIndex];
        }

        public void CreateItems(IReadOnlyList<InventoryCategorizedItemUiModel> categorizedItems)
        {
            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            
            _itemsByCategory.Clear();
            if (categorizedItems != null)
            {
                foreach (var categorizedItem in categorizedItems)
                {
                    if (!_itemsByCategory.TryGetValue(categorizedItem.Category, out var items))
                    {
                        items = new List<InventoryItemUiModel>();
                        _itemsByCategory[categorizedItem.Category] = items;
                    }

                    items.Add(categorizedItem.Item);
                }
            }

            RenderCurrentCategory();
        }

        private void RenderCurrentCategory()
        {
            _cardGroupsPool.DisableAll();
            
            if (!_itemsByCategory.TryGetValue(_currentCategory, out var data))
            {
                data = new List<InventoryItemUiModel>();
            }
            
            foreach (var item in data)
            {
                var inventoryView = _cardGroupsPool.GetNext();
                inventoryView.SetData(item);
                LoadContentSpritesSequentially(inventoryView, item.ItemId, _loadSpritesCts.Token).Forget();
            }
        }
        
        private async UniTask LoadContentSpritesSequentially(InventoryView itemView, string spriteId, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(spriteId);
                itemView.SetSprite(sprite);
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        public void Dispose()
        {
            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }
    }
}
