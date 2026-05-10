using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using VContainer;

namespace Game.Fishing
{
    [Window("FishingMinigameWindow", WindowType.Popup)]
    public sealed class FishingMinigameController : WindowController<FishingMinigameView>
    {
        private IFishingService _fishingService;

        private FishingMinigameArgs Args => (FishingMinigameArgs)Arguments;

        private bool _attemptCompletionDispatched;

        [Inject]
        private void Construct(IFishingService fishingService)
        {
            _fishingService = fishingService ?? throw new ArgumentNullException(nameof(fishingService));
        }

        protected override void OnShowStart()
        {
            View.Initialize(Args);
        }

        protected override void OnShowComplete()
        {
            View.ResolutionCommitted += OnResolutionCommitted;
            View.BeginRunning();
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.ResolutionCommitted -= OnResolutionCommitted;

            if (!_attemptCompletionDispatched && !Args.AttemptId.IsEmpty)
            {
                _attemptCompletionDispatched = true;
                CompletePendingAttemptAsFailedAsync(Args.AttemptId).Forget();
            }
        }

        private void OnResolutionCommitted(FishingMinigameResolution resolution)
        {
            if (_attemptCompletionDispatched)
                return;

            _attemptCompletionDispatched = true;
            ResolveAttemptAsync(resolution).Forget();
        }

        private async UniTaskVoid ResolveAttemptAsync(FishingMinigameResolution resolution)
        {
            View.ShowResolvingState();

            FishingCatchResult result = null;
            try
            {
                result = await _fishingService.CompleteFishingAsync(Args.AttemptId, resolution.IsSuccess, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameController] Completing fishing attempt '{Args.AttemptId}' failed. {exception}");
            }

            var isCatchSuccessful = result != null && result.Success;
            var title = BuildResultTitle(isCatchSuccessful, resolution.IsPerfect, resolution.IsTimeout);
            var details = BuildResultDetails(isCatchSuccessful, result);

            await View.ShowResultAsync(isCatchSuccessful, resolution.IsPerfect && isCatchSuccessful, title, details, CancellationToken.None);
            UIManager.Hide<FishingMinigameController>();
        }

        private async UniTaskVoid CompletePendingAttemptAsFailedAsync(FishingAttemptId attemptId)
        {
            try
            {
                await _fishingService.CompleteFishingAsync(attemptId, false, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingMinigameController] Failed to cancel pending attempt '{attemptId}'. {exception}");
            }
        }

        private string BuildResultTitle(bool isCatchSuccessful, bool isPerfect, bool isTimeout)
        {
            if (isCatchSuccessful)
                return isPerfect ? "Perfect catch!" : "Fish caught!";

            return isTimeout ? "Too slow" : "Missed";
        }

        private string BuildResultDetails(bool isCatchSuccessful, FishingCatchResult result)
        {
            if (isCatchSuccessful && result != null)
            {
                var fishName = string.IsNullOrWhiteSpace(Args.SelectedFish?.DisplayName) ? result.FishId : Args.SelectedFish.DisplayName;
                return $"{fishName}\n{result.Weight:0.##} kg - {result.State}";
            }

            if (result != null && result.Error == FishingError.MinigameFailed)
                return "Tap when the shrinking circle reaches the target.";

            return "Fishing failed. Try again.";
        }
    }
}
