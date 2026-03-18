using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using R3;

namespace Inventory.Implementation.UI
{
    public sealed class InventoryTabsPresenter : IDisposable
    {
        private readonly string _ownerId;
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryReadService _inventoryReadService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private IDisposable _inventoryChangedSubscription;
        private readonly List<InventoryCategoryTabViewModel> _tabs = new();
        
        public IReadOnlyList<InventoryCategoryTabViewModel> Tabs => _tabs;
        public string OwnerId => _ownerId;
        
        public InventoryTabsPresenter(
            string ownerId,
            IInventoryService inventoryService,
            IInventoryReadService inventoryReadService,
            IItemCategoryRegistry itemCategoryRegistry)
        {
            _ownerId = ownerId;
            _inventoryService = inventoryService;
            _inventoryReadService = inventoryReadService;
            _itemCategoryRegistry = itemCategoryRegistry;

            foreach (var category in _itemCategoryRegistry.GetAllCategories())
            {
                if (_tabs.Find(tab => tab.Category.CategoryId == category.CategoryId) != null)
                {
                    continue;
                }

                var tab = new InventoryCategoryTabViewModel(category);
                _tabs.Add(tab);
            }
        }
        
        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var tab in _tabs)
            {
                var source = await _inventoryReadService.GetItemsAsync(
                    _ownerId,
                    tab.Category.CategoryId,
                    cancellationToken);

                tab.Items.Value = MapItems(source, tab.Category);
            }

            _inventoryChangedSubscription?.Dispose();
            _inventoryChangedSubscription = _inventoryService.OnInventoryChanged
                .Where(evt => evt.OwnerId == _ownerId)
                .Subscribe(evt =>
                {
                    foreach (var tab in _tabs)
                    {
                        var items = evt.ItemsByCategory.TryGetValue(tab.Category.CategoryId, out var source)
                            ? source
                            : Array.Empty<InventoryItemView>();
                        tab.Items.Value = MapItems(items, tab.Category);
                    }
                });
        }

        private IReadOnlyList<InventoryItemUiModel> MapItems(
            IReadOnlyList<InventoryItemView> source,
            ItemCategory fallbackCategory)
        {
            var mapped = new List<InventoryItemUiModel>(source.Count);
            
            foreach (var item in source)
            {
                var category = _itemCategoryRegistry?.GetById(item.CategoryId) ?? fallbackCategory;
                var itemUIModel = new InventoryItemUiModel(item.ItemId, "Empty", item.StackCount, category);
                mapped.Add(itemUIModel);
            }

            return mapped;
        }

        public void Dispose()
        {
            _inventoryChangedSubscription?.Dispose();
            foreach (var tab in _tabs)
            {
                tab.Items?.Dispose();
            }
        }
    }
}
