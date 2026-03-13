using System;
using System.Collections.Generic;

namespace UIShared
{
    public abstract class ContentWidgetDataBase
    {
    }

    public sealed class ContentWidgetData : ContentWidgetDataBase
    {
        public static readonly ContentWidgetData Empty = new(Array.Empty<string>(), Array.Empty<ContentWidgetResourceData>());

        public readonly IReadOnlyList<string> CardPackAddresses;
        public readonly IReadOnlyList<ContentWidgetResourceData> Resources;

        public ContentWidgetData(
            IReadOnlyList<string> cardPackAddresses,
            IReadOnlyList<ContentWidgetResourceData> resources)
        {
            CardPackAddresses = cardPackAddresses ?? Array.Empty<string>();
            Resources = resources ?? Array.Empty<ContentWidgetResourceData>();
        }
    }

    public sealed class InventoryWidgetData : ContentWidgetDataBase
    {
        public string ItemId;
        public Action<string> ButtonPressed;
        
        public InventoryWidgetData(string itemId, Action<string> action)
        {
            ItemId = itemId;
            ButtonPressed = action;
        }
    }

    public readonly struct ContentWidgetResourceData
    {
        public readonly string Address;
        public readonly int Amount;

        public ContentWidgetResourceData(string address, int amount)
        {
            Address = address;
            Amount = amount;
        }
    }
}
