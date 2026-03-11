using System;
using System.Collections.Generic;
using Inventory.API;
using Inventory.Implementation.UI;
using R3;
using UISystem;

namespace Inventory.Implementation
{
    public class InventoryArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly InventoryTabsPresenter TabsPresenter;

        public InventoryArgs(UIManager uiManager, InventoryTabsPresenter tabsPresenter)
        {
            UiManager =  uiManager;
            TabsPresenter = tabsPresenter;
        }
    }
    
    [Window("InventoryWindow")]
    public class InventoryWindowController : WindowController<InventoryWindowView>
    {
        private InventoryArgs Args => (InventoryArgs) Arguments;
        private IDisposable _regularItemsSubscription;
        private IDisposable _cardPacksSubscription;

        protected override void OnShowStart()
        {
            View.CreateItems(BuildCategorizedItemsFromPresenter(Args.TabsPresenter));
            
            _regularItemsSubscription = Args.TabsPresenter.RegularItems.Items
                .Skip(1)
                .Subscribe(_ => RefreshFromPresenter());
            _cardPacksSubscription = Args.TabsPresenter.CardPacks.Items
                .Skip(1)
                .Subscribe(_ => RefreshFromPresenter());
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.Dispose();
            View.CloseClick -= CloseWindow;
            _regularItemsSubscription?.Dispose();
            _regularItemsSubscription = null;
            _cardPacksSubscription?.Dispose();
            _cardPacksSubscription = null;
            Args.TabsPresenter?.Dispose();
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<InventoryWindowController>();
        }

        private void RefreshFromPresenter()
        {
            if (Args.TabsPresenter == null)
            {
                return;
            }

            View.CreateItems(BuildCategorizedItemsFromPresenter(Args.TabsPresenter));
        }

        private static IReadOnlyList<InventoryCategorizedItemUiModel> BuildCategorizedItemsFromPresenter(
            InventoryTabsPresenter presenter)
        {
            if (presenter == null)
            {
                return Array.Empty<InventoryCategorizedItemUiModel>();
            }

            var regularItems = presenter.RegularItems.Items.Value;
            var cardPackItems = presenter.CardPacks.Items.Value;
            var mapped = new List<InventoryCategorizedItemUiModel>(regularItems.Count + cardPackItems.Count);

            foreach (var item in regularItems)
            {
                mapped.Add(new InventoryCategorizedItemUiModel(InventoryItemCategory.Regular, item));
            }

            foreach (var item in cardPackItems)
            {
                mapped.Add(new InventoryCategorizedItemUiModel(InventoryItemCategory.CardPack, item));
            }

            return mapped;
        }
    }
}
