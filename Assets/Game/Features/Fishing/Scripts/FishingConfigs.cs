using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Fishing
{
    public static class FishingConfigPaths
    {
        public const string RootFolder = "Fishing";
        public const string Fish = "fish.json";
        public const string Zones = "zones.json";
        public const string Lures = "lures.json";
        public const string Items = "items.json";
        public const string Settings = "fishing_settings.json";
        public const string CraftingRecipes = "crafting_recipes.json";
    }

    [Serializable]
    public sealed class FishConfigRoot
    {
        [JsonProperty("fish")] public List<FishConfig> Fish = new();
    }

    [Serializable]
    public sealed class FishingZonesConfigRoot
    {
        [JsonProperty("water_body_types")] public List<WaterBodyTypeConfig> WaterBodyTypes = new();
        [JsonProperty("fishing_zones")] public List<FishingZoneConfig> FishingZones = new();
    }

    [Serializable]
    public sealed class LuresConfigRoot
    {
        [JsonProperty("lures")] public List<LureConfig> Lures = new();
    }

    [Serializable]
    public sealed class FishingItemsConfigRoot
    {
        [JsonProperty("items")] public List<FishingItemConfig> Items = new();
    }

    [Serializable]
    public sealed class FishingSettingsConfigRoot
    {
        [JsonProperty("fishing_minigame")] public FishingMinigameConfig FishingMinigame;
        [JsonProperty("fish_behavior_settings")] public List<FishBehaviorSettingsConfig> FishBehaviorSettings = new();
        [JsonProperty("fish_book_tabs")] public List<FishBookTabConfig> FishBookTabs = new();
    }

    [Serializable]
    public sealed class FishingCraftingRecipesRefRoot
    {
        [JsonProperty("crafting_recipes")] public List<FishingCraftRecipeRef> CraftingRecipes = new();
    }

    [Serializable]
    public sealed class FishConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("available_lure_ids")] public List<string> AvailableLureIds = new();
        [JsonProperty("water_body_types")] public List<string> WaterBodyTypes = new();
        [JsonProperty("circle_size")] public string CircleSize;
        [JsonProperty("behavior_type")] public string BehaviorType;
        [JsonProperty("spawn_weight")] public int SpawnWeight;
        [JsonProperty("xp")] public int Xp;
        [JsonProperty("event_only")] public bool EventOnly;
        [JsonProperty("event_ids")] public List<string> EventIds = new();
        [JsonProperty("weight_thresholds")] public FishWeightThresholds WeightThresholds;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishWeightThresholds
    {
        [JsonProperty("common")] public float Common;
        [JsonProperty("rare")] public float Rare;
        [JsonProperty("epic")] public float Epic;
        [JsonProperty("legendary")] public float Legendary;
    }

    [Serializable]
    public sealed class FishingZoneConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("water_body_type")] public string WaterBodyType;
        [JsonProperty("is_unlock_feature_enabled")] public bool IsUnlockFeatureEnabled;
        [JsonProperty("is_unlocked_by_default")] public bool IsUnlockedByDefault;
        [JsonProperty("unlock_condition")] public JToken UnlockCondition;
        [JsonProperty("cooldown_enabled")] public bool CooldownEnabled;
        [JsonProperty("cooldown_seconds")] public int CooldownSeconds;
        [JsonProperty("allowed_lure_ids")] public List<string> AllowedLureIds = new();
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class WaterBodyTypeConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class LureConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("item_id")] public string ItemId;
        [JsonProperty("craft_recipe_id")] public string CraftRecipeId;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("sort_order")] public int SortOrder;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishingItemConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("type")] public string Type;
        [JsonProperty("stackable")] public bool Stackable;
        [JsonProperty("max_stack")] public int MaxStack;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishingMinigameConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("type")] public string Type;
        [JsonProperty("start_radius")] public float StartRadius;
        [JsonProperty("target_radius")] public float TargetRadius;
        [JsonProperty("end_radius")] public float EndRadius;
        [JsonProperty("shrink_duration_seconds")] public float ShrinkDurationSeconds;
        [JsonProperty("success_tolerance")] public float SuccessTolerance;
        [JsonProperty("max_attempts")] public int MaxAttempts;
        [JsonProperty("consume_lure_on_start")] public bool ConsumeLureOnStart;
        [JsonProperty("give_reward_on_success_only")] public bool GiveRewardOnSuccessOnly;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishBehaviorSettingsConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("minigame_speed_multiplier")] public float MinigameSpeedMultiplier;
        [JsonProperty("success_tolerance_multiplier")] public float SuccessToleranceMultiplier;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishBookTabConfig : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("filter")] public JObject Filter;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    public sealed class FishingCraftRecipeRef
    {
        [JsonProperty("id")] public string Id;
    }

    public interface IJsonExtensionCarrier
    {
        IDictionary<string, JToken> ExtensionData { get; set; }
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
