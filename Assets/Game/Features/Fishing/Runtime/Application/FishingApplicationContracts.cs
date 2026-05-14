using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Fishing
{
    public sealed class FishBookProgress
    {
        public string FishId;
        public bool IsDiscovered;
        public bool IsNew;
        public int CaughtCount;
        public float BestWeight;
        public List<string> UnlockedWeightStates = new();
    }

    public interface IFishingService
    {
        UniTask<FishingStartResult> StartFishingAsync(string zoneId, string lureId, CancellationToken ct = default);
        UniTask<FishingCatchResult> CompleteFishingAsync(FishingAttemptId attemptId, bool minigameSuccess, CancellationToken ct = default);
    }

    public interface IFishBookService
    {
        UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default);
        UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default);
        UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default);
    }

    public interface IFishCatchResolver
    {
        UniTask<FishingCatchResult> ResolveCatchAsync(string fishId, CancellationToken ct = default);
    }

    public interface ICaughtFishService
    {
        UniTask<FishingCatchResult> HandleCatchAsync(FishingCatchResult result, CancellationToken ct = default);
    }

    public interface ICaughtFishPresenter
    {
        void Present(FishingCatchResult result, FishBookProgress progress);
    }

    public interface IFishingInventoryGateway
    {
        UniTask<bool> HasItemAsync(string itemId, int amount, CancellationToken ct = default);
        UniTask<bool> RemoveItemAsync(string itemId, int amount, CancellationToken ct = default);
        UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default);
    }

    public interface IActiveFishingEventsProvider
    {
        IReadOnlyCollection<string> GetActiveEventIds();
    }

    public interface IFishingConfigContentSource
    {
        UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct);
    }

    public interface IFishingConfigProvider
    {
        UniTask<FishingStaticData> LoadAsync(CancellationToken ct);
        void ClearCache();
    }
}
