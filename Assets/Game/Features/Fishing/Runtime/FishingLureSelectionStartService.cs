using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Fishing
{
    public interface IFishingLureSelectionStartService
    {
        UniTask<FishingLureSelectionStartResult> TryStartAsync(string zoneId, string lureId, CancellationToken ct = default);
    }

    public sealed class FishingLureSelectionStartService : IFishingLureSelectionStartService
    {
        private readonly IFishingMinigameFacade _fishingMinigameFacade;

        public FishingLureSelectionStartService(IFishingMinigameFacade fishingMinigameFacade)
        {
            _fishingMinigameFacade = fishingMinigameFacade ?? throw new ArgumentNullException(nameof(fishingMinigameFacade));
        }

        public async UniTask<FishingLureSelectionStartResult> TryStartAsync(string zoneId, string lureId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(zoneId))
                return FishingLureSelectionStartResult.Fail(FishingError.ZoneNotFound);

            if (string.IsNullOrWhiteSpace(lureId))
                return FishingLureSelectionStartResult.Fail(FishingError.LureNotFound);

            Debug.LogWarning($"[FishingLureSelectionStartService] Opening fishing minigame. ZoneId='{zoneId}', LureId='{lureId}'.");
            var shown = await _fishingMinigameFacade.TryShowAsync(zoneId, lureId, ct);
            if (!shown)
            {
                Debug.LogWarning($"[FishingLureSelectionStartService] Fishing minigame open rejected. ZoneId='{zoneId}', LureId='{lureId}'.");
                return FishingLureSelectionStartResult.Fail(FishingError.None);
            }

            Debug.LogWarning($"[FishingLureSelectionStartService] Fishing minigame open dispatched. ZoneId='{zoneId}', LureId='{lureId}'.");
            return FishingLureSelectionStartResult.Ok();
        }
    }
}
