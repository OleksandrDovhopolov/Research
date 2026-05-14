using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class EmptyActiveFishingEventsProvider : IActiveFishingEventsProvider
    {
        private static readonly string[] Empty = Array.Empty<string>();

        public IReadOnlyCollection<string> GetActiveEventIds()
        {
            return Empty;
        }
    }

    public sealed class LoggingCaughtFishPresenter : ICaughtFishPresenter
    {
        public void Present(FishingCatchResult result, FishBookProgress progress)
        {
            if (result == null)
                return;

            var unlockedStates = progress?.UnlockedWeightStates == null || progress.UnlockedWeightStates.Count == 0
                ? "none"
                : string.Join(", ", progress.UnlockedWeightStates.Where(state => !string.IsNullOrWhiteSpace(state)));

            Debug.LogWarning(
                $"[CaughtFishPresenter] FishId='{result.FishId}', ItemId='{result.ItemId}', State={result.State}, Weight={result.Weight:0.##}, " +
                $"BestWeight={progress?.BestWeight ?? result.Weight:0.##}, CaughtCount={progress?.CaughtCount ?? 0}, UnlockedStates=[{unlockedStates}]");
        }
    }
}
