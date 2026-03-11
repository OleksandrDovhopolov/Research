using System;
using System.Collections.Generic;
using Inventory.Implementation.UI;
using R3;
using UISystem;

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
        private InventoryArgs Args => (InventoryArgs) Arguments;
        private readonly List<IDisposable> _tabSubscriptions = new();

        protected override void OnShowStart()
        {
            View.SetTabCategories(Args.Categories);
            View.CreateItems(BuildCategorizedItemsFromPresenter());

            _tabSubscriptions.Clear();
            foreach (var tab in Args.TabsPresenter.Tabs)
            {
                var subscription = tab.Items
                    .Skip(1)
                    .Subscribe(_ => RefreshFromPresenter());
                _tabSubscriptions.Add(subscription);
            }
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.Dispose();
            View.CloseClick -= CloseWindow;
            foreach (var subscription in _tabSubscriptions)
            {
                subscription?.Dispose();
            }
            _tabSubscriptions.Clear();
            Args.TabsPresenter?.Dispose();
        }

        private void RefreshFromPresenter()
        {
            if (Args.TabsPresenter == null)
            {
                return;
            }

            View.CreateItems(BuildCategorizedItemsFromPresenter());
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
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<InventoryWindowController>();
        }
    }
}
