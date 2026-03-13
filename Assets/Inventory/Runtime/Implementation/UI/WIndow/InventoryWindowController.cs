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
        public readonly InventoryTabsPresenter TabsPresenter;
        public readonly IReadOnlyList<API.ItemCategory> Categories;

        public InventoryArgs(
            UIManager uiManager,
            InventoryTabsPresenter tabsPresenter,
            IReadOnlyList<API.ItemCategory> categories)
        {
            UiManager =  uiManager;
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
            View.TabClicked += OnTabClicked;

            var filteredItemsSubscription = Observable
                .CombineLatest(SelectedCategoryId, RawItems, FilterItemsByCategory)
                .Subscribe(items => View.Render(items));
            _subscriptions.Add(filteredItemsSubscription);
            
            RawItems.Value = BuildCategorizedItemsFromPresenter();

            foreach (var tab in Args.TabsPresenter.Tabs)
            {
                var subscription = tab.Items
                    .Skip(1)
                    .Subscribe(_ => RefreshFromPresenter());
                _subscriptions.Add(subscription);
            }
        }

        protected override void OnShowComplete()
        {
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
            Args.TabsPresenter?.Dispose();
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
        
        private void OnOpenableViewClickedHandler(InventoryView view)
        {
            OpenItemAsync(view, View.GetWindowLifetimeToken()).Forget();
        }
        
        private async UniTaskVoid OpenItemAsync(InventoryView view, CancellationToken cancellationToken)
        {
            if (!view.IsOpenable)
            {
                Debug.LogWarning($"Category is not IOpenable");
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inventoryData = new InventoryWidgetData();
                var args = new ContentWidgetArgs(Args.UiManager, inventoryData, view.RectTransform);
                Args.UiManager.Show<ContentWidgetController>(args);
                //await openable.OpenAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryWindowView] Failed to open item '{view.ItemId}'. {exception}");
            }
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
