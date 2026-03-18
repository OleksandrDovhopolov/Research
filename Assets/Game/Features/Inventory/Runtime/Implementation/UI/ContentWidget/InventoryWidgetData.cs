using System;
using UIShared;

namespace Inventory.Implementation
{
    public class InventoryWidgetData : ContentWidgetDataBase
    {
        public string ItemId;
        public Action<string> ButtonPressed;
        
        public InventoryWidgetData(string itemId, Action<string> action)
        {
            ItemId = itemId;
            ButtonPressed = action;
        }
    }
}
