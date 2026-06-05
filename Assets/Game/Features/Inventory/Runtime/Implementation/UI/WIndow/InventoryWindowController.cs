using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.UI;
using R3;
using UIShared;
using UISystem;
using UnityEngine;

namespace Inventory.Implementation
{
    public class InventoryArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly IInventoryItemUseService InventoryItemUseService;
        public readonly InventoryTabsPresenter TabsPresenter;
        public readonly IReadOnlyList<ItemCategory> Categories;

        public InventoryArgs(
            UIManager uiManager,
            IInventoryItemUseService inventoryItemUseService,
            InventoryTabsPresenter tabsPresenter,
            IReadOnlyList<ItemCategory> categories)
        {
            UiManager =  uiManager;
            InventoryItemUseService =  inventoryItemUseService;
            TabsPresenter = tabsPresenter;
            Categories = categories;
        }
    }
    
    [Window("InventoryWindow")]
    public class InventoryWindowController : WindowController<InventoryWindowView>
    {
        private const string AllCategoriesTabId = "__all__";
        
        private InventoryArgs Args => (InventoryArgs) Arguments;
        private readonly List<IDisposable> _subscriptions = new();
        public readonly ReactiveProperty<string> SelectedCategoryId = new(AllCategoriesTabId);
        public readonly ReactiveProperty<IReadOnlyList<InventoryCategorizedItemUiModel>> RawItems = new(Array.Empty<InventoryCategorizedItemUiModel>());

        protected override void OnShowStart()
        {
            View.FocusTabVisual(0);

            var filteredItemsSubscription = Observable
                .CombineLatest(SelectedCategoryId, RawItems, FilterItemsByCategory)
                .Subscribe(items => View.Render(items));
            _subscriptions.Add(filteredItemsSubscription);

            RawItems.Value = BuildCategorizedItemsFromPresenter();
            LogInventoryContents(RawItems.Value);

            foreach (var tab in Args.TabsPresenter.Tabs)
            {
                var subscription = tab.Items
                    .Skip(1)
                    .Subscribe(_ => RefreshFromPresenter());
                _subscriptions.Add(subscription);
            }
        }

        private void LogInventoryContents(IReadOnlyList<InventoryCategorizedItemUiModel> items)
        {
            var ownerId = Args?.TabsPresenter?.OwnerId ?? "<unknown>";
            if (items == null || items.Count == 0)
            {
                Debug.Log($"[InventoryWindow] Opened. OwnerId='{ownerId}'. Inventory is empty.");
                return;
            }

            var totalStacks = items.Count;
            var totalUnits = 0;
            var withIcon = 0;
            var withoutIcon = 0;
            var perCategory = new Dictionary<string, int>(StringComparer.Ordinal);

            var ids = new System.Text.StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                var entry = items[i];
                var stackCount = entry.Item.StackCount;
                totalUnits += stackCount;

                if (entry.Item.Icon != null) withIcon++;
                else withoutIcon++;

                if (!perCategory.TryAdd(entry.CategoryId, 1))
                {
                    perCategory[entry.CategoryId]++;
                }

                if (ids.Length > 0) ids.Append(", ");
                var iconMark = entry.Item.Icon != null ? "spec" : "addr";
                ids.Append($"{entry.Item.ItemId}(x{stackCount}, cat={entry.CategoryId}, icon={iconMark})");
            }

            var categoriesBreakdown = string.Join(
                ", ",
                perCategory.OrderBy(p => p.Key, StringComparer.Ordinal)
                           .Select(p => $"{p.Key}={p.Value}"));

            Debug.Log(
                $"[InventoryWindow] Opened. OwnerId='{ownerId}'. " +
                $"Stacks={totalStacks}, Units={totalUnits}, " +
                $"IconsFromRewardSpec={withIcon}, IconsFromAddressables={withoutIcon}. " +
                $"Categories: {categoriesBreakdown}. " +
                $"Items: [{ids}]");
        }

        protected override void OnShowComplete()
        {
            View.TabClicked += OnTabClicked;
            View.CloseClick += CloseWindow;
            View.BackgroundClicked += TryHideContentWidget;
            View.OnOpenableViewClicked += OnOpenableViewClickedHandler;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            TryHideContentWidget();
            
            View.Dispose();
            View.TabClicked -= OnTabClicked;
            View.CloseClick -= CloseWindow;
            View.BackgroundClicked -= TryHideContentWidget;
            View.OnOpenableViewClicked -= OnOpenableViewClickedHandler;
            
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();
            SelectedCategoryId.Value = AllCategoriesTabId;
            RawItems.Value = Array.Empty<InventoryCategorizedItemUiModel>();

            if (isClosed)
            {
                Args.TabsPresenter?.Dispose();
            }
        }

        #region R3 Filtration
        
        private void RefreshFromPresenter()
        {
            RawItems.Value = BuildCategorizedItemsFromPresenter();
        }

        private IReadOnlyList<InventoryCategorizedItemUiModel> BuildCategorizedItemsFromPresenter()
        {
            var mapped = new List<InventoryCategorizedItemUiModel>();
            foreach (var tab in Args.TabsPresenter.Tabs)
            {
                foreach (var item in tab.Items.Value)
                {
                    mapped.Add(new InventoryCategorizedItemUiModel(tab.Category.CategoryId, item));
                }
            }

            return mapped;
        }
        
        private void OnTabClicked(int tabIndex)
        {
            TryHideContentWidget();
            
            SelectedCategoryId.Value = ResolveCategoryId(tabIndex);
        }
        
        private string ResolveCategoryId(int tabIndex)
        {
            if (tabIndex == 0)
            {
                return AllCategoriesTabId;
            }

            var categoryIndex = tabIndex - 1;
            if (categoryIndex < 0 || categoryIndex >= Args.Categories.Count)
            {
                return string.Empty;
            }

            return Args.Categories[categoryIndex]?.CategoryId ?? string.Empty;
        }
        
        private static List<InventoryItemUiModel> FilterItemsByCategory(
            string selectedCategoryId,
            IReadOnlyList<InventoryCategorizedItemUiModel> rawItems)
        {
            if (rawItems == null || rawItems.Count == 0)
            {
                return new List<InventoryItemUiModel>();
            }

            if (selectedCategoryId == AllCategoriesTabId)
            {
                return rawItems.Select(item => item.Item).ToList();
            }

            return rawItems
                .Where(item => item.CategoryId == selectedCategoryId)
                .Select(item => item.Item)
                .ToList();
        }
        
        #endregion
        
        #region IOpenable
        
        private InventoryView _selectedView;
        
        private void OnOpenableViewClickedHandler(InventoryView view)
        {
            ContentWidgetDataBase data;
            
            switch (view.InventoryItemUiModel.Category.GetMetadata())
            {
                case ActionWidgetMetadata:
                    data = new InventoryWidgetData(
                        view.ItemId,
                        itemId => OnInventoryButtonClickedHandler(itemId, View.GetWindowLifetimeToken()).Forget());
                    break;
                case ResourceWidgetMetadata:
                    data = new InventoryResourceWidgetData(
                        view.ItemId,
                        view.Sprite,
                        view.InventoryItemUiModel.StackCount,
                        itemId =>
                        {
                            TryHideContentWidget();
                            Debug.LogWarning($"Debug closed content widget");
                        });
                    break;
                default:
                    Debug.LogWarning($"Category is not supported");
                    return;
            }
            
            _selectedView = view;
            
            try
            {
                View.GetWindowLifetimeToken().ThrowIfCancellationRequested();

                var args = new ContentWidgetArgs(data, view.RectTransform);
                Args.UiManager.Show<ContentWidgetController>(args);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryWindowView] Failed to open item '{view.ItemId}'. {exception}");
            }
        }
        

        private async UniTask OnInventoryButtonClickedHandler(string itemId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            View.ShowLoader(true);

            try
            {
                TryHideContentWidget();
                var itemDelta = BuildInventoryItemDelta(itemId);
                await Args.InventoryItemUseService.ConsumeItemAsync(itemDelta, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryWindowController] Failed to remove item '{itemId}'. {exception}");
            }
            finally
            {
                View.ShowLoader(false);
            }
        }

        private InventoryItemDelta BuildInventoryItemDelta(string itemId)
        {
            var selectedModel = _selectedView?.InventoryItemUiModel;
            if (selectedModel == null)
            {
                throw new InvalidOperationException("Failed to build InventoryItemDelta. Selected view is null.");
            }

            return new InventoryItemDelta(
                Args.TabsPresenter.OwnerId,
                itemId,
                1,
                selectedModel.Value.Category);
        }
        
        #endregion
        
        private void TryHideContentWidget()
        {
            if (Args.UiManager.IsWindowShown<ContentWidgetController>())
            {
                Args.UiManager.Hide<ContentWidgetController>();
            }
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<InventoryWindowController>();
        }
    }
}
