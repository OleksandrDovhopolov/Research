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
        private readonly List<ItemCategory> _tabCategories = new();
        private string _currentCategoryId = string.Empty;
        private readonly Dictionary<string, List<InventoryItemUiModel>> _itemsByCategory = new();

        private void Start()
        {
            if (_tabs.Count > 0)
            {
                FocusTab(0, false);
            }
        }

        public void SetTabCategories(IReadOnlyList<ItemCategory> categories)
        {
            _tabCategories.Clear();
            if (categories != null)
            {
                foreach (var category in categories)
                {
                    if (category == null)
                    {
                        continue;
                    }

                    _tabCategories.Add(category);
                }
            }

            _currentCategoryId = _tabCategories.Count > 0
                ? _tabCategories[0].CategoryId
                : string.Empty;
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

            FocusTab(tabIndex);
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
            _currentCategoryId = ResolveCategoryId(tabIndex);

            if (shouldRender)
            {
                RenderCurrentCategory();
            }
        }

        private string ResolveCategoryId(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabCategories.Count)
            {
                return _tabCategories.Count > 0 ? _tabCategories[0].CategoryId : string.Empty;
            }

            return _tabCategories[tabIndex].CategoryId;
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
                    if (!_itemsByCategory.TryGetValue(categorizedItem.CategoryId, out var items))
                    {
                        items = new List<InventoryItemUiModel>();
                        _itemsByCategory[categorizedItem.CategoryId] = items;
                    }

                    items.Add(categorizedItem.Item);
                }
            }

            RenderCurrentCategory();
        }

        private void RenderCurrentCategory()
        {
            _cardGroupsPool.DisableAll();
            
            if (!_itemsByCategory.TryGetValue(_currentCategoryId, out var data))
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
