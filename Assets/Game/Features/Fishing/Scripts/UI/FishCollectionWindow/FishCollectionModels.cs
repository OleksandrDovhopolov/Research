using System;
using System.Collections.Generic;
using UISystem;

namespace Game.Fishing
{
    public sealed class FishCollectionArgs : WindowArgs
    {
        public FishCollectionArgs(IReadOnlyList<FishCollectionEntryViewData> entries)
        {
            Entries = entries ?? Array.Empty<FishCollectionEntryViewData>();
        }

        public IReadOnlyList<FishCollectionEntryViewData> Entries { get; }
    }

    public sealed class FishCollectionEntryViewData
    {
        public FishCollectionEntryViewData(
            string fishId,
            string spriteAddress,
            string displayName,
            string waterBodyTypesText,
            string behaviorType,
            string itemType,
            float minWeight,
            float maxWeight,
            FishBookProgress progress)
        {
            FishId = fishId ?? string.Empty;
            SpriteAddress = spriteAddress ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            WaterBodyTypesText = waterBodyTypesText ?? string.Empty;
            BehaviorType = behaviorType ?? string.Empty;
            ItemType = itemType ?? string.Empty;
            MinWeight = minWeight;
            MaxWeight = maxWeight;
            Progress = progress;
        }

        public string FishId { get; }
        public string SpriteAddress { get; }
        public string DisplayName { get; }
        public string WaterBodyTypesText { get; }
        public string BehaviorType { get; }
        public string ItemType { get; }
        public float MinWeight { get; }
        public float MaxWeight { get; }
        public FishBookProgress Progress { get; }
    }
}
