using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;

namespace Game.Fishing
{
    public sealed class SaveBackedFishBookService : IFishBookService
    {
        private const int CommonStateMask = 1 << 0;
        private const int RareStateMask = 1 << 1;
        private const int EpicStateMask = 1 << 2;
        private const int LegendaryStateMask = 1 << 3;
        private readonly SaveService _saveService;

        public SaveBackedFishBookService(SaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public async UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.FishId))
                return;

            await _saveService.UpdateModuleAsync(data => data.Fishing, fishing =>
            {
                fishing.CaughtFish ??= new List<CaughtFishSaveData>();

                var caughtFish = fishing.CaughtFish.FirstOrDefault(x => string.Equals(x.FishId, result.FishId, StringComparison.Ordinal));
                if (caughtFish == null)
                {
                    caughtFish = new CaughtFishSaveData
                    {
                        FishId = result.FishId,
                        IsNew = true,
                        CaughtCount = 0,
                        BestWeight = 0f,
                        CaughtStatesMask = 0
                    };
                    fishing.CaughtFish.Add(caughtFish);
                }

                caughtFish.CaughtCount += 1;
                caughtFish.IsNew = true;
                caughtFish.BestWeight = Math.Max(caughtFish.BestWeight, result.Weight);
                caughtFish.CaughtStatesMask |= ToStateMask(result.State);
            }, ct);
        }

        public async UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fishId))
                return null;

            var fishing = await _saveService.GetReadonlyModuleAsync(data => data.Fishing, ct);
            var caughtFish = fishing?.CaughtFish?.FirstOrDefault(x => string.Equals(x.FishId, fishId, StringComparison.Ordinal));
            return MapToProgress(caughtFish);
        }

        public async UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fishId))
                return;

            await _saveService.UpdateModuleAsync(data => data.Fishing, fishing =>
            {
                var caughtFish = fishing?.CaughtFish?.FirstOrDefault(x => string.Equals(x.FishId, fishId, StringComparison.Ordinal));
                if (caughtFish == null)
                    return;

                caughtFish.IsNew = false;
            }, ct);
        }

        private static FishBookProgress MapToProgress(CaughtFishSaveData caughtFish)
        {
            if (caughtFish == null || string.IsNullOrWhiteSpace(caughtFish.FishId))
                return null;

            return new FishBookProgress
            {
                FishId = caughtFish.FishId,
                IsDiscovered = true,
                IsNew = caughtFish.IsNew,
                CaughtCount = Math.Max(0, caughtFish.CaughtCount),
                BestWeight = Math.Max(0f, caughtFish.BestWeight),
                UnlockedWeightStates = GetUnlockedStates(caughtFish.CaughtStatesMask)
            };
        }

        private static int ToStateMask(FishWeightState state)
        {
            return state switch
            {
                FishWeightState.Legendary => LegendaryStateMask,
                FishWeightState.Epic => EpicStateMask,
                FishWeightState.Rare => RareStateMask,
                _ => CommonStateMask
            };
        }

        private static List<string> GetUnlockedStates(int caughtStatesMask)
        {
            var states = new List<string>(4);
            if ((caughtStatesMask & CommonStateMask) != 0)
                states.Add("common");
            if ((caughtStatesMask & RareStateMask) != 0)
                states.Add("rare");
            if ((caughtStatesMask & EpicStateMask) != 0)
                states.Add("epic");
            if ((caughtStatesMask & LegendaryStateMask) != 0)
                states.Add("legendary");

            return states;
        }
    }
}
