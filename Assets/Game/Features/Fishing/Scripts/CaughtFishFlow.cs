using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class ConfigBackedFishCatchResolver : IFishCatchResolver
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishWeightService _fishWeightService;

        public ConfigBackedFishCatchResolver(IFishingConfigProvider configProvider, IFishWeightService fishWeightService)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishWeightService = fishWeightService ?? throw new ArgumentNullException(nameof(fishWeightService));
        }

        public async UniTask<FishingCatchResult> ResolveCatchAsync(string fishId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fishId))
                return FishingCatchResult.Fail(FishingError.ConfigInvalid);

            var data = await _configProvider.LoadAsync(ct);
            if (!data.FishById.TryGetValue(fishId, out var fishConfig) || fishConfig == null)
                return FishingCatchResult.Fail(FishingError.ConfigInvalid);

            var roll = _fishWeightService.RollWeight(fishConfig);
            return FishingCatchResult.Ok(fishConfig.Id, FishingStaticData.GetFishItemId(fishConfig.Id), roll.Weight, roll.State);
        }
    }

    public sealed class CaughtFishService : ICaughtFishService
    {
        private readonly IFishBookService _fishBookService;
        private readonly ICaughtFishPresenter _caughtFishPresenter;

        public CaughtFishService(IFishBookService fishBookService, ICaughtFishPresenter caughtFishPresenter)
        {
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
            _caughtFishPresenter = caughtFishPresenter ?? throw new ArgumentNullException(nameof(caughtFishPresenter));
        }

        public async UniTask<FishingCatchResult> HandleCatchAsync(FishingCatchResult result, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.FishId))
                return result ?? FishingCatchResult.Fail(FishingError.ConfigInvalid);

            await _fishBookService.RegisterCatchAsync(result, ct);
            var progress = await _fishBookService.GetProgressAsync(result.FishId, ct);

            try
            {
                _caughtFishPresenter.Present(result, progress);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CaughtFishService] Failed to present caught fish '{result.FishId}'. {exception}");
            }

            return result;
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
