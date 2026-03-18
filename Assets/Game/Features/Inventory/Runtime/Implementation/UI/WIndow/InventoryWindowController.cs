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
            View.TabClicked += OnTabClicked;
            View.CloseClick += CloseWindow;
            View.BackgroundClicked += TryHideContentWidget;
            View.OnOpenableViewClicked += OnOpenableViewClickedHandler;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            Debug.LogWarning($"Debug OnHideStart isClosed {isClosed}");
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

        protected override void OnHideComplete(bool isClosed)
        {
            base.OnHideComplete(isClosed);
            Debug.LogWarning($"Debug {GetType().Name} OnHideComplete");
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

                var args = new ContentWidgetArgs(Args.UiManager, data, view.RectTransform);
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
