using System;
using System.Collections.Generic;
using System.Linq;
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
    internal sealed class FishConfigRootDto
    {
        [JsonProperty("fish")] public List<FishConfigDto> Fish = new();
    }

    [Serializable]
    internal sealed class FishingZonesConfigRootDto
    {
        [JsonProperty("water_body_types")] public List<WaterBodyTypeConfigDto> WaterBodyTypes = new();
        [JsonProperty("fishing_zones")] public List<FishingZoneConfigDto> FishingZones = new();
    }

    [Serializable]
    internal sealed class LuresConfigRootDto
    {
        [JsonProperty("lures")] public List<LureConfigDto> Lures = new();
    }

    [Serializable]
    internal sealed class FishingItemsConfigRootDto
    {
        [JsonProperty("items")] public List<FishingItemConfigDto> Items = new();
    }

    [Serializable]
    internal sealed class FishingSettingsConfigRootDto
    {
        [JsonProperty("fishing_minigame")] public FishingMinigameConfigDto FishingMinigame;
        [JsonProperty("fish_behavior_settings")] public List<FishBehaviorSettingsConfigDto> FishBehaviorSettings = new();
        [JsonProperty("fish_book_tabs")] public List<FishBookTabConfigDto> FishBookTabs = new();
    }

    [Serializable]
    internal sealed class FishingCraftingRecipesRefRootDto
    {
        [JsonProperty("crafting_recipes")] public List<FishingCraftRecipeRefDto> CraftingRecipes = new();
    }

    [Serializable]
    internal sealed class FishConfigDto : IJsonExtensionCarrier
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
        [JsonProperty("weight_thresholds")] public FishWeightThresholdsDto WeightThresholds;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    internal sealed class FishWeightThresholdsDto
    {
        [JsonProperty("common")] public float Common;
        [JsonProperty("rare")] public float Rare;
        [JsonProperty("epic")] public float Epic;
        [JsonProperty("legendary")] public float Legendary;
    }

    [Serializable]
    internal sealed class FishingZoneConfigDto : IJsonExtensionCarrier
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
    internal sealed class WaterBodyTypeConfigDto : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    internal sealed class LureConfigDto : IJsonExtensionCarrier
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
    internal sealed class FishingItemConfigDto : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("type")] public string Type;
        [JsonProperty("stackable")] public bool Stackable;
        [JsonProperty("max_stack")] public int MaxStack;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    internal sealed class FishingMinigameConfigDto : IJsonExtensionCarrier
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
    internal sealed class FishBehaviorSettingsConfigDto : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("minigame_speed_multiplier")] public float MinigameSpeedMultiplier;
        [JsonProperty("success_tolerance_multiplier")] public float SuccessToleranceMultiplier;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    internal sealed class FishBookTabConfigDto : IJsonExtensionCarrier
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("display_name")] public string DisplayName;
        [JsonProperty("filter")] public JObject Filter;
        [JsonExtensionData] public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [Serializable]
    internal sealed class FishingCraftRecipeRefDto
    {
        [JsonProperty("id")] public string Id;
    }

    internal interface IJsonExtensionCarrier
    {
        IDictionary<string, JToken> ExtensionData { get; set; }
    }

    internal static class FishingConfigMapper
    {
        public static FishingStaticData ToStaticData(
            FishConfigRootDto fishRoot,
            FishingZonesConfigRootDto zonesRoot,
            LuresConfigRootDto luresRoot,
            FishingItemsConfigRootDto itemsRoot,
            FishingSettingsConfigRootDto settingsRoot,
            FishingCraftingRecipesRefRootDto recipeRefsRoot)
        {
            var recipeIds = (recipeRefsRoot?.CraftingRecipes ?? new List<FishingCraftRecipeRefDto>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .Select(x => x.Id)
                .ToArray();

            return new FishingStaticData(
                MapFish(fishRoot?.Fish),
                MapZones(zonesRoot?.FishingZones),
                MapWaterBodyTypes(zonesRoot?.WaterBodyTypes),
                MapLures(luresRoot?.Lures),
                MapItems(itemsRoot?.Items),
                MapSettings(settingsRoot),
                recipeIds);
        }

        private static IReadOnlyList<FishConfig> MapFish(IReadOnlyList<FishConfigDto> source)
        {
            return source?.Select(dto => dto == null
                ? null
                : new FishConfig
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    AvailableLureIds = CloneList(dto.AvailableLureIds),
                    WaterBodyTypes = CloneList(dto.WaterBodyTypes),
                    CircleSize = dto.CircleSize,
                    BehaviorType = dto.BehaviorType,
                    SpawnWeight = dto.SpawnWeight,
                    Xp = dto.Xp,
                    EventOnly = dto.EventOnly,
                    EventIds = CloneList(dto.EventIds),
                    WeightThresholds = dto.WeightThresholds == null
                        ? null
                        : new FishWeightThresholds
                        {
                            Common = dto.WeightThresholds.Common,
                            Rare = dto.WeightThresholds.Rare,
                            Epic = dto.WeightThresholds.Epic,
                            Legendary = dto.WeightThresholds.Legendary
                        },
                    Metadata = MapMetadata(dto)
                }).ToArray() ?? Array.Empty<FishConfig>();
        }

        private static IReadOnlyList<FishingZoneConfig> MapZones(IReadOnlyList<FishingZoneConfigDto> source)
        {
            return source?.Select(dto => dto == null
                ? null
                : new FishingZoneConfig
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    WaterBodyType = dto.WaterBodyType,
                    IsUnlockFeatureEnabled = dto.IsUnlockFeatureEnabled,
                    IsUnlockedByDefault = dto.IsUnlockedByDefault,
                    UnlockCondition = dto.UnlockCondition?.ToObject<object>(),
                    CooldownEnabled = dto.CooldownEnabled,
                    CooldownSeconds = dto.CooldownSeconds,
                    AllowedLureIds = CloneList(dto.AllowedLureIds),
                    Metadata = MapMetadata(dto)
                }).ToArray() ?? Array.Empty<FishingZoneConfig>();
        }

        private static IReadOnlyList<WaterBodyTypeConfig> MapWaterBodyTypes(IReadOnlyList<WaterBodyTypeConfigDto> source)
        {
            return source?.Select(dto => dto == null
                ? null
                : new WaterBodyTypeConfig
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    Metadata = MapMetadata(dto)
                }).ToArray() ?? Array.Empty<WaterBodyTypeConfig>();
        }

        private static IReadOnlyList<LureConfig> MapLures(IReadOnlyList<LureConfigDto> source)
        {
            return source?.Select(dto => dto == null
                ? null
                : new LureConfig
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    ItemId = dto.ItemId,
                    CraftRecipeId = dto.CraftRecipeId,
                    Rarity = dto.Rarity,
                    SortOrder = dto.SortOrder,
                    Metadata = MapMetadata(dto)
                }).ToArray() ?? Array.Empty<LureConfig>();
        }

        private static IReadOnlyList<FishingItemConfig> MapItems(IReadOnlyList<FishingItemConfigDto> source)
        {
            return source?.Select(dto => dto == null
                ? null
                : new FishingItemConfig
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    Type = dto.Type,
                    Stackable = dto.Stackable,
                    MaxStack = dto.MaxStack,
                    Metadata = MapMetadata(dto)
                }).ToArray() ?? Array.Empty<FishingItemConfig>();
        }

        private static FishingSettingsConfigRoot MapSettings(FishingSettingsConfigRootDto source)
        {
            if (source == null)
                return new FishingSettingsConfigRoot();

            return new FishingSettingsConfigRoot
            {
                FishingMinigame = source.FishingMinigame == null
                    ? null
                    : new FishingMinigameConfig
                    {
                        Id = source.FishingMinigame.Id,
                        Type = source.FishingMinigame.Type,
                        StartRadius = source.FishingMinigame.StartRadius,
                        TargetRadius = source.FishingMinigame.TargetRadius,
                        EndRadius = source.FishingMinigame.EndRadius,
                        ShrinkDurationSeconds = source.FishingMinigame.ShrinkDurationSeconds,
                        SuccessTolerance = source.FishingMinigame.SuccessTolerance,
                        MaxAttempts = source.FishingMinigame.MaxAttempts,
                        ConsumeLureOnStart = source.FishingMinigame.ConsumeLureOnStart,
                        GiveRewardOnSuccessOnly = source.FishingMinigame.GiveRewardOnSuccessOnly,
                        Metadata = MapMetadata(source.FishingMinigame)
                    },
                FishBehaviorSettings = source.FishBehaviorSettings?.Select(dto => dto == null
                    ? null
                    : new FishBehaviorSettingsConfig
                    {
                        Id = dto.Id,
                        MinigameSpeedMultiplier = dto.MinigameSpeedMultiplier,
                        SuccessToleranceMultiplier = dto.SuccessToleranceMultiplier,
                        Metadata = MapMetadata(dto)
                    }).ToList() ?? new List<FishBehaviorSettingsConfig>(),
                FishBookTabs = source.FishBookTabs?.Select(dto => dto == null
                    ? null
                    : new FishBookTabConfig
                    {
                        Id = dto.Id,
                        DisplayName = dto.DisplayName,
                        Filter = dto.Filter?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>(),
                        Metadata = MapMetadata(dto)
                    }).ToList() ?? new List<FishBookTabConfig>()
            };
        }

        private static List<string> CloneList(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
                return new List<string>();

            return source.Where(value => value != null).ToList();
        }

        private static Dictionary<string, object> MapMetadata(IJsonExtensionCarrier carrier)
        {
            if (carrier?.ExtensionData == null || carrier.ExtensionData.Count == 0)
                return new Dictionary<string, object>();

            return carrier.ExtensionData.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.ToObject<object>(),
                StringComparer.Ordinal);
        }
    }
}
