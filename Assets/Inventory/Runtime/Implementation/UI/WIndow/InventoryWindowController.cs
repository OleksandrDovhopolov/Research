using System.Collections.Generic;
using Inventory.Implementation.UI;
using UISystem;

namespace Inventory.Implementation
{
    public class InventoryArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly IReadOnlyList<InventoryItemUiModel> Items;

        public InventoryArgs(UIManager uiManager, IReadOnlyList<InventoryItemUiModel> items)
        {
            UiManager =  uiManager;
            Items = items;
        }
    }
    
    [Window("InventoryWindow")]
    public class InventoryWindowController : WindowController<InventoryWindowView>
    {
        private InventoryArgs Args => (InventoryArgs) Arguments;

        protected override void OnShowStart()
        {
            View.CreateItems(Args.Items);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<InventoryWindowController>();
        }
    }
}
