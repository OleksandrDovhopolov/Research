using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Fishing
{
    public interface IFishingLureSelectionHudFacade
    {
        UniTask<bool> TryShowAsync(string zoneId, CancellationToken ct = default);
    }

    public interface IFishingLureSelectionHudActions
    {
        UniTask<FishingLureSelectionStartResult> TryStartFishingAsync(string zoneId, string lureId, CancellationToken ct = default);
        void HideHud();
        void ShowInfo(string message);
    }

    public interface IFishingLureSelectionStartService
    {
        UniTask<FishingLureSelectionStartResult> TryStartAsync(string zoneId, string lureId, CancellationToken ct = default);
    }

    public sealed class FishingLureSelectionStartResult
    {
        public bool Success { get; private set; }
        public FishingError Error { get; private set; }

        public static FishingLureSelectionStartResult Ok()
        {
            return new FishingLureSelectionStartResult
            {
                Success = true,
                Error = FishingError.None
            };
        }

        public static FishingLureSelectionStartResult Fail(FishingError error)
        {
            return new FishingLureSelectionStartResult
            {
                Success = false,
                Error = error
            };
        }
    }

    public sealed class FishingLureSelectionRenderArgs
    {
        public FishingLureSelectionRenderArgs(
            string zoneId,
            IReadOnlyList<FishingHudLureRenderData> lures,
            IReadOnlyCollection<string> allowedLureIds)
        {
            ZoneId = zoneId ?? string.Empty;
            Lures = lures ?? System.Array.Empty<FishingHudLureRenderData>();
            AllowedLureIds = allowedLureIds ?? System.Array.Empty<string>();
        }

        public string ZoneId { get; }
        public IReadOnlyList<FishingHudLureRenderData> Lures { get; }
        public IReadOnlyCollection<string> AllowedLureIds { get; }
    }
}
