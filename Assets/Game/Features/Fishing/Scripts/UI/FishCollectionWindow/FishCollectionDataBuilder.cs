using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Game.Fishing
{
    public interface IFishCollectionDataBuilder
    {
        UniTask<FishCollectionArgs> BuildAsync(CancellationToken ct = default);
    }

    public sealed class FishCollectionDataBuilder : IFishCollectionDataBuilder
    {
        private const string FishItemType = "fish";

        private readonly IFishingConfigContentSource _contentSource;
        private readonly IFishBookService _fishBookService;

        public FishCollectionDataBuilder(IFishingConfigContentSource contentSource, IFishBookService fishBookService)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
        }

        public async UniTask<FishCollectionArgs> BuildAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var fishRoot = await LoadAsync<FishConfigRoot>(FishingConfigPaths.Fish, ct);
            var zonesRoot = await LoadAsync<FishingZonesConfigRoot>(FishingConfigPaths.Zones, ct);
            var waterBodyTypesById = (zonesRoot.WaterBodyTypes ?? new List<WaterBodyTypeConfig>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            var fishList = fishRoot.Fish ?? new List<FishConfig>();
            var entries = new List<FishCollectionEntryViewData>(fishList.Count);

            foreach (var fish in fishList)
            {
                ct.ThrowIfCancellationRequested();
                if (fish == null || fish.EventOnly)
                    continue;

                var progress = await _fishBookService.GetProgressAsync(fish.Id, ct);
                var waterBodyTypes = ResolveWaterBodyTypes(waterBodyTypesById, fish);
                var (minWeight, maxWeight) = CalculateWeightRange(fish);

                entries.Add(new FishCollectionEntryViewData(
                    fish.Id,
                    fish.Id,
                    fish.DisplayName,
                    waterBodyTypes,
                    fish.BehaviorType,
                    FishItemType,
                    minWeight,
                    maxWeight,
                    progress));
            }

            return new FishCollectionArgs(entries);
        }

        internal static (float MinWeight, float MaxWeight) CalculateWeightRange(FishConfig fishConfig)
        {
            if (fishConfig?.WeightThresholds == null)
                return (0f, 0f);

            var minWeight = Math.Max(0.01f, fishConfig.WeightThresholds.Common * 0.75f);
            var maxWeight = Math.Max(minWeight, fishConfig.WeightThresholds.Legendary * 1.25f);
            return (RoundWeight(minWeight), RoundWeight(maxWeight));
        }

        internal static string FormatWeight(float weight)
        {
            return weight.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static float RoundWeight(float weight)
        {
            return (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero);
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
                throw new InvalidOperationException($"Failed to parse fish collection config '{relativePath}': {exception.Message}", exception);
            }
        }

        private static string ResolveWaterBodyTypes(
            IReadOnlyDictionary<string, WaterBodyTypeConfig> waterBodyTypesById,
            FishConfig fish)
        {
            if (fish?.WaterBodyTypes == null || fish.WaterBodyTypes.Count == 0)
                return string.Empty;

            var names = new List<string>(fish.WaterBodyTypes.Count);
            foreach (var waterBodyTypeId in fish.WaterBodyTypes)
            {
                if (string.IsNullOrWhiteSpace(waterBodyTypeId))
                    continue;

                if (waterBodyTypesById.TryGetValue(waterBodyTypeId, out var waterBodyType) &&
                    !string.IsNullOrWhiteSpace(waterBodyType?.DisplayName))
                {
                    names.Add(waterBodyType.DisplayName);
                }
                else
                {
                    names.Add(waterBodyTypeId);
                }
            }

            return string.Join(", ", names.Where(name => !string.IsNullOrWhiteSpace(name)));
        }
    }
}
