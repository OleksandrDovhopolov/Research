using System;
using UIShared;
using UnityEngine;

namespace Inventory.Implementation
{
    public class InventoryResourceWidgetData : ContentWidgetDataBase
    {
        public string ItemId;
        public int ItemAmount;
        public Sprite ItemSprite;
        public Action<string> ButtonPressed;
        
        public InventoryResourceWidgetData(string itemId, Sprite sprite, int amount, Action<string> action)
        {
            ItemId = itemId;
            ItemSprite = sprite;
            ItemAmount = amount;
            ButtonPressed = action;
        }
    }
}
