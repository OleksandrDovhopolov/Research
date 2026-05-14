using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Crafting;

namespace Game.Fishing
{
    public interface IFishingHudFacade
    {
        UniTask<bool> TryShowAsync(CancellationToken ct = default);
    }

    public interface IFishingHudActions
    {
        UniTask<CraftStartResult> StartCraftAsync(string craftRecipeId, CancellationToken ct = default);
        UniTask<CraftTask> GetActiveCraftAsync(CancellationToken ct = default);
        UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default);
        UniTask<CraftCollectResult> CompleteAndCollectAsync(CraftTaskId taskId, CancellationToken ct = default);
        UniTask<IReadOnlyList<FishingHudLureRenderData>> GetLureRenderDataAsync(CancellationToken ct = default);
        UniTask<bool> TrySpendSpeedUpGemsAsync(int amount, CancellationToken ct = default);
        UniTask RefundSpeedUpGemsAsync(int amount, CancellationToken ct = default);
        DateTimeOffset GetCurrentTimeUtc();
        void HideHud();
        void ShowInfo(string message);
    }
}
