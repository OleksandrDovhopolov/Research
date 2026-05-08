using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Fishing
{
    public interface IFishCollectionDataBuilder
    {
        UniTask<FishCollectionArgs> BuildAsync(CancellationToken ct = default);
    }

    public sealed class FishCollectionDataBuilder : IFishCollectionDataBuilder
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishBookService _fishBookService;

        public FishCollectionDataBuilder(IFishingConfigProvider configProvider, IFishBookService fishBookService)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
        }

        public async UniTask<FishCollectionArgs> BuildAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var data = await _configProvider.LoadAsync(ct);
            var entries = new List<FishCollectionEntryViewData>(data.Fish.Count);

            foreach (var fish in data.Fish)
            {
                ct.ThrowIfCancellationRequested();
                if (fish == null)
                    continue;

                var progress = await _fishBookService.GetProgressAsync(fish.Id, ct);
                var itemId = FishingStaticData.GetFishItemId(fish.Id);
                data.ItemsById.TryGetValue(itemId, out var itemConfig);

                var waterBodyTypes = ResolveWaterBodyTypes(data, fish);
                var (minWeight, maxWeight) = CalculateWeightRange(fish);

                entries.Add(new FishCollectionEntryViewData(
                    fish.Id,
                    itemId,
                    fish.DisplayName,
                    waterBodyTypes,
                    fish.BehaviorType,
                    itemConfig?.Type,
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

        private static string ResolveWaterBodyTypes(FishingStaticData data, FishConfig fish)
        {
            if (fish?.WaterBodyTypes == null || fish.WaterBodyTypes.Count == 0)
                return string.Empty;

            var names = new List<string>(fish.WaterBodyTypes.Count);
            foreach (var waterBodyTypeId in fish.WaterBodyTypes)
            {
                if (string.IsNullOrWhiteSpace(waterBodyTypeId))
                    continue;

                if (data.WaterBodyTypesById.TryGetValue(waterBodyTypeId, out var waterBodyType) &&
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
