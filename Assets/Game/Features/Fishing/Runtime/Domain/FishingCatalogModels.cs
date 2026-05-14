using System;
using System.Collections.Generic;

namespace Game.Fishing
{
    public sealed class FishConfig
    {
        public string Id;
        public string DisplayName;
        public List<string> AvailableLureIds = new();
        public List<string> WaterBodyTypes = new();
        public string CircleSize;
        public string BehaviorType;
        public int SpawnWeight;
        public int Xp;
        public bool EventOnly;
        public List<string> EventIds = new();
        public FishWeightThresholds WeightThresholds;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishWeightThresholds
    {
        public float Common;
        public float Rare;
        public float Epic;
        public float Legendary;
    }

    public sealed class FishingZoneConfig
    {
        public string Id;
        public string DisplayName;
        public string WaterBodyType;
        public bool IsUnlockFeatureEnabled;
        public bool IsUnlockedByDefault;
        public object UnlockCondition;
        public bool CooldownEnabled;
        public int CooldownSeconds;
        public List<string> AllowedLureIds = new();
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class WaterBodyTypeConfig
    {
        public string Id;
        public string DisplayName;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class LureConfig
    {
        public string Id;
        public string DisplayName;
        public string ItemId;
        public string CraftRecipeId;
        public string Rarity;
        public int SortOrder;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishingItemConfig
    {
        public string Id;
        public string DisplayName;
        public string Type;
        public bool Stackable;
        public int MaxStack;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishingMinigameConfig
    {
        public string Id;
        public string Type;
        public float StartRadius;
        public float TargetRadius;
        public float EndRadius;
        public float ShrinkDurationSeconds;
        public float SuccessTolerance;
        public int MaxAttempts;
        public bool ConsumeLureOnStart;
        public bool GiveRewardOnSuccessOnly;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishBehaviorSettingsConfig
    {
        public string Id;
        public float MinigameSpeedMultiplier;
        public float SuccessToleranceMultiplier;
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishBookTabConfig
    {
        public string Id;
        public string DisplayName;
        public Dictionary<string, object> Filter = new();
        public Dictionary<string, object> Metadata = new();
    }

    public sealed class FishingSettingsConfigRoot
    {
        public FishingMinigameConfig FishingMinigame;
        public List<FishBehaviorSettingsConfig> FishBehaviorSettings = new();
        public List<FishBookTabConfig> FishBookTabs = new();
    }

    public sealed class FishingStaticData
    {
        public FishingStaticData(
            IReadOnlyList<FishConfig> fish,
            IReadOnlyList<FishingZoneConfig> zones,
            IReadOnlyList<WaterBodyTypeConfig> waterBodyTypes,
            IReadOnlyList<LureConfig> lures,
            IReadOnlyList<FishingItemConfig> items,
            FishingSettingsConfigRoot settings,
            IReadOnlyCollection<string> craftRecipeIds)
        {
            Fish = fish ?? Array.Empty<FishConfig>();
            Zones = zones ?? Array.Empty<FishingZoneConfig>();
            WaterBodyTypes = waterBodyTypes ?? Array.Empty<WaterBodyTypeConfig>();
            Lures = lures ?? Array.Empty<LureConfig>();
            Items = items ?? Array.Empty<FishingItemConfig>();
            Settings = settings ?? new FishingSettingsConfigRoot();
            CraftRecipeIds = craftRecipeIds ?? Array.Empty<string>();

            FishById = BuildDictionary(Fish, x => x.Id);
            ZonesById = BuildDictionary(Zones, x => x.Id);
            WaterBodyTypesById = BuildDictionary(WaterBodyTypes, x => x.Id);
            LuresById = BuildDictionary(Lures, x => x.Id);
            ItemsById = BuildDictionary(Items, x => x.Id);
        }

        public IReadOnlyList<FishConfig> Fish { get; }
        public IReadOnlyList<FishingZoneConfig> Zones { get; }
        public IReadOnlyList<WaterBodyTypeConfig> WaterBodyTypes { get; }
        public IReadOnlyList<LureConfig> Lures { get; }
        public IReadOnlyList<FishingItemConfig> Items { get; }
        public FishingSettingsConfigRoot Settings { get; }
        public IReadOnlyCollection<string> CraftRecipeIds { get; }
        public IReadOnlyDictionary<string, FishConfig> FishById { get; }
        public IReadOnlyDictionary<string, FishingZoneConfig> ZonesById { get; }
        public IReadOnlyDictionary<string, WaterBodyTypeConfig> WaterBodyTypesById { get; }
        public IReadOnlyDictionary<string, LureConfig> LuresById { get; }
        public IReadOnlyDictionary<string, FishingItemConfig> ItemsById { get; }

        public static string GetFishItemId(string fishId)
        {
            return string.IsNullOrWhiteSpace(fishId) ? string.Empty : "item_" + fishId;
        }

        private static IReadOnlyDictionary<string, T> BuildDictionary<T>(IReadOnlyList<T> values, Func<T, string> keySelector)
        {
            var dictionary = new Dictionary<string, T>(StringComparer.Ordinal);
            if (values == null)
                return dictionary;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value == null)
                    continue;

                var key = keySelector(value);
                if (!string.IsNullOrWhiteSpace(key))
                    dictionary[key] = value;
            }

            return dictionary;
        }
    }
}
