using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
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
            _attemptCompletionDispatched = false;
            Debug.LogWarning($"[FishingMinigameController] OnShowStart. ZoneId='{Args.ZoneId}', AttemptId='{Args.AttemptId}', FishId='{Args.SelectedFish?.Id ?? string.Empty}'.");
            View.Initialize(Args);
        }

        protected override void OnShowComplete()
        {
            View.ResolutionCommitted += OnResolutionCommitted;
            Debug.LogWarning($"[FishingMinigameController] OnShowComplete. AttemptId='{Args.AttemptId}'.");
            View.BeginRunning();
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.ResolutionCommitted -= OnResolutionCommitted;
            Debug.LogWarning($"[FishingMinigameController] OnHideStart. AttemptId='{Args.AttemptId}', IsClosed={isClosed}, CompletionDispatched={_attemptCompletionDispatched}.");

            if (!_attemptCompletionDispatched && !Args.AttemptId.IsEmpty)
            {
                _attemptCompletionDispatched = true;
                Debug.LogWarning($"[FishingMinigameController] Attempt unresolved before hide. Completing as failed. AttemptId='{Args.AttemptId}'.");
                CompletePendingAttemptAsFailedAsync(Args.AttemptId).Forget();
            }
        }

        private void OnResolutionCommitted(FishingMinigameResolution resolution)
        {
            if (_attemptCompletionDispatched)
            {
                Debug.LogWarning($"[FishingMinigameController] Resolution ignored because completion was already dispatched. AttemptId='{Args.AttemptId}'.");
                return;
            }

            _attemptCompletionDispatched = true;
            Debug.LogWarning($"[FishingMinigameController] Resolution committed. AttemptId='{Args.AttemptId}', Success={resolution.IsSuccess}, Perfect={resolution.IsPerfect}, Timeout={resolution.IsTimeout}, Radius={resolution.CurrentRadius:0.###}.");
            ResolveAttemptAsync(resolution).Forget();
        }

        private async UniTaskVoid ResolveAttemptAsync(FishingMinigameResolution resolution)
        {
            View.ShowResolvingState();
            Debug.LogWarning($"[FishingMinigameController] Resolving attempt. AttemptId='{Args.AttemptId}', MinigameSuccess={resolution.IsSuccess}.");

            FishingCatchResult result = null;
            try
            {
                result = await _fishingService.CompleteFishingAsync(Args.AttemptId, resolution.IsSuccess, CancellationToken.None);
                Debug.LogWarning($"[FishingMinigameController] CompleteFishingAsync finished. AttemptId='{Args.AttemptId}', Success={result?.Success ?? false}, Error={result?.Error ?? FishingError.ConfigInvalid}, FishId='{result?.FishId ?? string.Empty}', Weight={result?.Weight ?? 0f:0.##}, State={(result != null ? result.State.ToString() : string.Empty)}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameController] Completing fishing attempt '{Args.AttemptId}' failed. {exception}");
            }

            var isCatchSuccessful = result != null && result.Success;
            var title = BuildResultTitle(isCatchSuccessful, resolution.IsPerfect, resolution.IsTimeout);

            ShowInfoResult(title);
            await View.ShowResultAsync(isCatchSuccessful, resolution.IsPerfect && isCatchSuccessful, CancellationToken.None);
            Debug.LogWarning($"[FishingMinigameController] Result shown. AttemptId='{Args.AttemptId}', CatchSuccessful={isCatchSuccessful}, Title='{title}'.");
            UIManager.Hide<FishingMinigameController>();
        }

        private async UniTaskVoid CompletePendingAttemptAsFailedAsync(FishingAttemptId attemptId)
        {
            try
            {
                await _fishingService.CompleteFishingAsync(attemptId, false, CancellationToken.None);
                Debug.LogWarning($"[FishingMinigameController] Pending attempt completed as failed. AttemptId='{attemptId}'.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingMinigameController] Failed to cancel pending attempt '{attemptId}'. {exception}");
            }
        }

        private string BuildResultTitle(bool isCatchSuccessful, bool isPerfect, bool isTimeout)
        {
            return isCatchSuccessful ? "Success" : "Fail";
        }

        private void ShowInfoResult(string title)
        {
            UIManager.Show<InfoWidgetController>(new InfoWidgetArg(title));
        }
    }
}
