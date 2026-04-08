using UnityEngine;

namespace CardCollection.Core
{
    public struct RewardViewData
    {
        public static RewardViewData Empty => new(string.Empty, null, 0);

        public readonly string Id;
        public readonly Sprite Icon;
        public readonly int Amount;

        public RewardViewData(string id, Sprite icon, int amount)
        {
            Id = id;
            Icon = icon;
            Amount = amount;
        }
    }
}
