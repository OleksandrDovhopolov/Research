using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Fishing
{
    public interface IFishingConfigContentSource
    {
        UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct);
    }

    public interface IFishingConfigProvider
    {
        UniTask<FishingStaticData> LoadAsync(CancellationToken ct);
        void ClearCache();
    }

    public sealed class StreamingAssetsFishingConfigContentSource : IFishingConfigContentSource
    {
        private readonly string _rootFolder;

        public StreamingAssetsFishingConfigContentSource()
        {
            _rootFolder = FishingConfigPaths.RootFolder;
        }

        public async UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Config relative path is empty.", nameof(relativePath));

            var fullPath = Path.Combine(Application.streamingAssetsPath, _rootFolder, relativePath);
            if (Application.platform == RuntimePlatform.Android)
            {
                using var request = UnityWebRequest.Get(fullPath);
                await request.SendWebRequest().WithCancellation(ct);

                if (request.result != UnityWebRequest.Result.Success)
                    throw new FileNotFoundException($"Fishing config file not found via WebRequest: {fullPath}. Error: {request.error}");

                return request.downloadHandler.text;
            }

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Fishing config file not found: {fullPath}");

            return await File.ReadAllTextAsync(fullPath, ct);
        }
    }

    public sealed class JsonFishingConfigProvider : IFishingConfigProvider
    {
        private readonly IFishingConfigContentSource _contentSource;
        private readonly FishingConfigValidator _validator;
        private FishingStaticData _cachedData;

        public JsonFishingConfigProvider(IFishingConfigContentSource contentSource, FishingConfigValidator validator)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async UniTask<FishingStaticData> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_cachedData != null)
                return _cachedData;

            var fishRoot = await LoadAsync<FishConfigRoot>(FishingConfigPaths.Fish, ct);
            var zonesRoot = await LoadAsync<FishingZonesConfigRoot>(FishingConfigPaths.Zones, ct);
            var luresRoot = await LoadAsync<LuresConfigRoot>(FishingConfigPaths.Lures, ct);
            var itemsRoot = await LoadAsync<FishingItemsConfigRoot>(FishingConfigPaths.Items, ct);
            var settingsRoot = await LoadAsync<FishingSettingsConfigRoot>(FishingConfigPaths.Settings, ct);
            var recipeRefsRoot = await LoadAsync<FishingCraftingRecipesRefRoot>(FishingConfigPaths.CraftingRecipes, ct);

            var recipeIds = (recipeRefsRoot.CraftingRecipes ?? new List<FishingCraftRecipeRef>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .Select(x => x.Id)
                .ToArray();

            var staticData = new FishingStaticData(
                fishRoot.Fish,
                zonesRoot.FishingZones,
                zonesRoot.WaterBodyTypes,
                luresRoot.Lures,
                itemsRoot.Items,
                settingsRoot,
                recipeIds);

            var errors = _validator.Validate(staticData);
            if (errors.Count > 0)
                throw new InvalidOperationException("Fishing config validation failed: " + string.Join("; ", errors));

            _cachedData = staticData;
            return _cachedData;
        }

        public void ClearCache()
        {
            _cachedData = null;
        }

        private async UniTask<T> LoadAsync<T>(string relativePath, CancellationToken ct) where T : class, new()
        {
            var json = await _contentSource.LoadJsonAsync(relativePath, ct);
            if (string.IsNullOrWhiteSpace(json))
                return new T();

            try
            {
                return JsonConvert.DeserializeObject<T>(json) ?? new T();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to parse fishing config '{relativePath}': {exception.Message}", exception);
            }
        }
    }

    public sealed class FishingConfigValidator
    {
        public IReadOnlyList<string> Validate(FishingStaticData data)
        {
            var errors = new List<string>();
            if (data == null)
            {
                errors.Add("Static data is null.");
                return errors;
            }

            ValidateUnique(data.Fish, x => x?.Id, "fish", errors);
            ValidateUnique(data.Zones, x => x?.Id, "zone", errors);
            ValidateUnique(data.Lures, x => x?.Id, "lure", errors);
            ValidateUnique(data.Items, x => x?.Id, "item", errors);
            ValidateUnique(data.WaterBodyTypes, x => x?.Id, "water_body_type", errors);

            var lureIds = new HashSet<string>(data.LuresById.Keys, StringComparer.Ordinal);
            var waterTypeIds = new HashSet<string>(data.WaterBodyTypesById.Keys, StringComparer.Ordinal);
            var itemIds = new HashSet<string>(data.ItemsById.Keys, StringComparer.Ordinal);
            var recipeIds = new HashSet<string>(data.CraftRecipeIds ?? Array.Empty<string>(), StringComparer.Ordinal);

            foreach (var lure in data.Lures)
            {
                if (lure == null)
                    continue;

                if (string.IsNullOrWhiteSpace(lure.ItemId) || !itemIds.Contains(lure.ItemId))
                    errors.Add($"Lure '{lure.Id}' references missing item_id '{lure.ItemId}'.");

                if (string.IsNullOrWhiteSpace(lure.CraftRecipeId) || !recipeIds.Contains(lure.CraftRecipeId))
                    errors.Add($"Lure '{lure.Id}' references missing craft_recipe_id '{lure.CraftRecipeId}'.");
            }

            foreach (var zone in data.Zones)
            {
                if (zone == null)
                    continue;

                if (string.IsNullOrWhiteSpace(zone.WaterBodyType) || !waterTypeIds.Contains(zone.WaterBodyType))
                    errors.Add($"Zone '{zone.Id}' references missing water_body_type '{zone.WaterBodyType}'.");

                ValidateReferences(zone.AllowedLureIds, lureIds, $"Zone '{zone.Id}' allowed_lure_ids", errors);
            }

            foreach (var fish in data.Fish)
            {
                if (fish == null)
                    continue;

                ValidateReferences(fish.AvailableLureIds, lureIds, $"Fish '{fish.Id}' available_lure_ids", errors);
                ValidateReferences(fish.WaterBodyTypes, waterTypeIds, $"Fish '{fish.Id}' water_body_types", errors);

                if (!itemIds.Contains(FishingStaticData.GetFishItemId(fish.Id)))
                    errors.Add($"Fish '{fish.Id}' item '{FishingStaticData.GetFishItemId(fish.Id)}' is missing.");

                if (fish.SpawnWeight <= 0)
                    errors.Add($"Fish '{fish.Id}' spawn_weight must be greater than 0.");

                if (fish.WeightThresholds == null)
                    errors.Add($"Fish '{fish.Id}' weight_thresholds are missing.");
                else if (!AreThresholdsOrdered(fish.WeightThresholds))
                    errors.Add($"Fish '{fish.Id}' weight_thresholds must be ordered common <= rare <= epic <= legendary.");

                if (fish.EventOnly && (fish.EventIds == null || fish.EventIds.Count == 0))
                    errors.Add($"Fish '{fish.Id}' is event_only but event_ids are missing.");
            }

            return errors;
        }

        private static void ValidateUnique<T>(IReadOnlyList<T> values, Func<T, string> idSelector, string label, List<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (values == null)
                return;

            foreach (var value in values)
            {
                var id = idSelector(value);
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"{label} has empty id.");
                    continue;
                }

                if (!ids.Add(id))
                    errors.Add($"{label} id '{id}' is duplicated.");
            }
        }

        private static void ValidateReferences(IEnumerable<string> values, HashSet<string> knownIds, string label, List<string> errors)
        {
            if (values == null)
            {
                errors.Add($"{label} are missing.");
                return;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || !knownIds.Contains(value))
                    errors.Add($"{label} references missing id '{value}'.");
            }
        }

        private static bool AreThresholdsOrdered(FishWeightThresholds thresholds)
        {
            return thresholds.Common <= thresholds.Rare &&
                   thresholds.Rare <= thresholds.Epic &&
                   thresholds.Epic <= thresholds.Legendary;
        }
    }
}
