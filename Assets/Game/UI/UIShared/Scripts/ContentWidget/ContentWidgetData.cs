using System;
using System.Collections.Generic;

namespace UIShared
{
    public sealed class ContentWidgetData
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
