using System;
using System.Collections.Generic;
using System.Linq;
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
        private IDisposable _inventoryChangedSubscription;
        private readonly List<InventoryCategoryTabViewModel> _tabs = new();
        
        public IReadOnlyList<InventoryCategoryTabViewModel> Tabs => _tabs;
        
        public InventoryTabsPresenter(IInventoryService inventoryService, string ownerId, IReadOnlyList<ItemCategory> categories)
        {
            _inventoryService = inventoryService;
            _ownerId = ownerId;

            foreach (var category in categories.Where(category => category != null))
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
                var source = await _inventoryService.GetItemsAsync(
                    _ownerId,
                    tab.Category.CategoryId,
                    cancellationToken);

                tab.Items.Value = MapItems(source);
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
                        tab.Items.Value = MapItems(items);
                    }
                });
        }

        private IReadOnlyList<InventoryItemUiModel> MapItems(IReadOnlyList<InventoryItemView> source)
        {
            var mapped = new List<InventoryItemUiModel>(source.Count);
            
            foreach (var item in source)
            {
                var itemUIModel = new InventoryItemUiModel(item.ItemId, "Empty", item.StackCount);
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
