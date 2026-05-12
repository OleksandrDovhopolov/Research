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
        private IFishBookService _fishBookService;

        private FishingMinigameArgs Args => (FishingMinigameArgs)Arguments;

        private bool _attemptCompletionDispatched;

        [Inject]
        private void Construct(IFishingService fishingService, IFishBookService fishBookService)
        {
            _fishingService = fishingService ?? throw new ArgumentNullException(nameof(fishingService));
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
        }

        protected override void OnShowStart()
        {
            _attemptCompletionDispatched = false;
            _result = null;
            _newFishArgs = null;
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
            Debug.LogWarning($"[FishingMinigameController] Resolution committed. AttemptId='{Args.AttemptId}', Success={resolution.IsSuccess}, Perfect={resolution.IsPerfect}, Timeout={resolution.IsTimeout}, EndReason={resolution.EndReason}, Radius={resolution.CurrentRadius:0.###}.");
            ResolveAttemptAsync(resolution).Forget();
        }

        private FishingCatchResult _result;
        private NewFishArgs _newFishArgs;

        private async UniTaskVoid ResolveAttemptAsync(FishingMinigameResolution resolution)
        {
            View.ShowResolvingState();
            Debug.LogWarning($"[FishingMinigameController] Resolving attempt. AttemptId='{Args.AttemptId}', MinigameSuccess={resolution.IsSuccess}.");

            try
            {
                _result = await _fishingService.CompleteFishingAsync(Args.AttemptId, resolution.IsSuccess, CancellationToken.None);
                if (_result is { Success: true })
                {
                    _newFishArgs = await BuildNewFishArgsAsync(_result);
                }

                Debug.LogWarning($"[FishingMinigameController] CompleteFishingAsync finished. AttemptId='{Args.AttemptId}', Success={_result?.Success ?? false}, Error={_result?.Error ?? FishingError.ConfigInvalid}, FishId='{_result?.FishId ?? string.Empty}', Weight={_result?.Weight ?? 0f:0.##}, State={(_result != null ? _result.State.ToString() : string.Empty)}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameController] Completing fishing attempt '{Args.AttemptId}' failed. {exception}");
            }

            var isCatchSuccessful = _result is { Success: true };
            var title = BuildResultTitle(isCatchSuccessful, resolution.IsPerfect, resolution.IsTimeout);

            ShowInfoResult(title);
            await View.ShowResultAsync(isCatchSuccessful, resolution.IsPerfect && isCatchSuccessful, CancellationToken.None);
            Debug.LogWarning($"[FishingMinigameController] Result shown. AttemptId='{Args.AttemptId}', CatchSuccessful={isCatchSuccessful}, Title='{title}'.");
            UIManager.Hide<FishingMinigameController>();
        }

        protected override void OnHideComplete(bool isClosed)
        {
            base.OnHideComplete(isClosed);
            if (_newFishArgs != null)
                UIManager.Show<NewFishController>(_newFishArgs);
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

        private async UniTask<NewFishArgs> BuildNewFishArgsAsync(FishingCatchResult result)
        {
            FishBookProgress progress = null;

            try
            {
                progress = await _fishBookService.GetProgressAsync(result.FishId, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameController] Failed to load fish progress for '{result.FishId}'. {exception}");
            }

            return NewFishArgs.FromCatchResult(result, progress);
        }
    }
}
