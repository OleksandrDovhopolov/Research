using System;

namespace Game.Fishing
{
    public sealed class FishWeightService : IFishWeightService
    {
        private readonly IFishingRandom _random;

        public FishWeightService(IFishingRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public FishWeightRollResult RollWeight(FishConfig fishConfig)
        {
            if (fishConfig?.WeightThresholds == null)
                return new FishWeightRollResult(0f, FishWeightState.Common);

            var min = Math.Max(0.01f, fishConfig.WeightThresholds.Common * 0.75f);
            var max = Math.Max(min, fishConfig.WeightThresholds.Legendary * 1.25f);
            var weight = min + (float)_random.NextDouble() * (max - min);
            var rounded = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero);
            return new FishWeightRollResult(rounded, GetState(fishConfig, rounded));
        }

        public FishWeightState GetState(FishConfig fishConfig, float weight)
        {
            var thresholds = fishConfig?.WeightThresholds;
            if (thresholds == null)
                return FishWeightState.Common;

            if (weight >= thresholds.Legendary)
                return FishWeightState.Legendary;
            if (weight >= thresholds.Epic)
                return FishWeightState.Epic;
            if (weight >= thresholds.Rare)
                return FishWeightState.Rare;

            return FishWeightState.Common;
        }
    }
}
