using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using VContainer;

namespace BattlePass
{
    [Window("BattlePassWindow")]
    public class BattlePassWindowController : WindowController<BattlePassView>
    {
        private IBattlePassServerService _battlePassServerService;
        private IBattlePassTimerService _battlePassTimerService;
        private BattlePassUiModelFactory _uiModelFactory;

        private CancellationTokenSource _loadCts;
        private BattlePassSnapshot _currentSnapshot;
        private bool _isClaimInFlight;

        [Inject]
        private void Construct(
            IBattlePassServerService battlePassServerService,
            IBattlePassTimerService battlePassTimerService,
            BattlePassUiModelFactory uiModelFactory)
        {
            _battlePassServerService = battlePassServerService;
            _battlePassTimerService = battlePassTimerService;
            _uiModelFactory = uiModelFactory;
        }

        protected override void OnShowStart()
        {
            ResetLoadCts();
            SubscribeTimer();
            View.ResetView();
            View.SetClaimButtonsInteractable(true);
            LoadBattlePassAsync(_loadCts.Token).Forget();
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.BuyPremiumClick += HandleBuyPremiumClicked;
            View.BuyPlatinumClick += HandleBuyPlatinumClicked;
            View.RewardClaimClick += HandleRewardClaimClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.BuyPlatinumClick -= HandleBuyPlatinumClicked;
            View.BuyPremiumClick -= HandleBuyPremiumClicked;
            View.RewardClaimClick -= HandleRewardClaimClicked;
            View.CloseClick -= CloseWindow;

            CancelLoad();
            UnsubscribeTimer();
            _battlePassTimerService?.Stop();
            View.ResetView();
            _currentSnapshot = null;
            _isClaimInFlight = false;
        }

        private async UniTaskVoid LoadBattlePassAsync(CancellationToken ct)
        {
            try
            {
                var snapshot = await _battlePassServerService.GetCurrentAsync(ct);
                ct.ThrowIfCancellationRequested();

                if (snapshot?.Season == null)
                {
                    _currentSnapshot = snapshot;
                    View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                    return;
                }

                ApplySnapshot(snapshot);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Failed to load Battle Pass data. {exception}");
                View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
            }
        }

        private void HandleTimerUpdated(TimeSpan remaining)
        {
            View.SetTimer(remaining);
        }

        private void HandleBuyPremiumClicked()
        {
        }

        private void HandleBuyPlatinumClicked()
        {
        }

        private void HandleRewardClaimClicked(int level, BattlePassRewardTrack rewardTrack)
        {
            if (_isClaimInFlight)
            {
                return;
            }

            if (!TryGetClaimableCell(level, rewardTrack, out var seasonId))
            {
                Debug.LogWarning($"[BattlePassWindowController] Claim ignored for unavailable reward cell level={level}, track={rewardTrack}.");
                return;
            }

            ClaimRewardAsync(seasonId, level, rewardTrack, _loadCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid ClaimRewardAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct)
        {
            _isClaimInFlight = true;
            View.SetClaimButtonsInteractable(false);

            try
            {
                var claimResult = await _battlePassServerService.ClaimAsync(seasonId, level, rewardTrack, ct);
                ct.ThrowIfCancellationRequested();

                if (claimResult != null && claimResult.Success)
                {
                    if (TryApplyClaimUserState(claimResult.UpdatedUserState))
                    {
                        return;
                    }

                    Debug.LogError("[BattlePassWindowController] Claim returned success, but updated user state is missing.");
                    await ReloadCurrentAsync(ct);
                    return;
                }

                Debug.LogError($"[BattlePassWindowController] Claim failed. Code={claimResult?.ErrorCode ?? "<none>"}, Message={claimResult?.ErrorMessage ?? "<none>"}");
                await ReloadCurrentAsync(ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Claim request failed. {exception}");
                await ReloadCurrentAsync(ct);
            }
            finally
            {
                _isClaimInFlight = false;
                View.SetClaimButtonsInteractable(true);
            }
        }

        private async UniTask ReloadCurrentAsync(CancellationToken ct)
        {
            try
            {
                var refreshedSnapshot = await _battlePassServerService.GetCurrentAsync(ct);
                ct.ThrowIfCancellationRequested();

                if (refreshedSnapshot?.Season == null)
                {
                    _currentSnapshot = refreshedSnapshot;
                    View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                    _battlePassTimerService?.Stop();
                    return;
                }

                ApplySnapshot(refreshedSnapshot);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Failed to reload Battle Pass state after claim. {exception}");
                View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                _battlePassTimerService?.Stop();
            }
        }

        private void ApplySnapshot(BattlePassSnapshot snapshot)
        {
            _currentSnapshot = snapshot;

            if (snapshot?.Season == null)
            {
                View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                _battlePassTimerService?.Stop();
                return;
            }

            var uiModel = _uiModelFactory.Create(snapshot);
            View.Render(uiModel);
            View.SetClaimButtonsInteractable(!_isClaimInFlight);
            _battlePassTimerService?.Start(snapshot.ServerTimeUtc, snapshot.Season.EndAtUtc);
        }

        private bool TryApplyClaimUserState(BattlePassUserState updatedUserState)
        {
            if (updatedUserState == null || _currentSnapshot?.Season == null)
            {
                return false;
            }

            var mergedSnapshot = new BattlePassSnapshot(
                _currentSnapshot.Season,
                _currentSnapshot.Products,
                updatedUserState,
                _currentSnapshot.Levels,
                _currentSnapshot.ServerTimeUtc);

            ApplySnapshot(mergedSnapshot);
            return true;
        }

        private bool TryGetClaimableCell(int level, BattlePassRewardTrack rewardTrack, out string seasonId)
        {
            seasonId = _currentSnapshot?.Season?.Id;
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return false;
            }

            return _currentSnapshot.UserState?.ClaimableRewards?.Any(cell =>
                cell != null &&
                cell.Level == level &&
                cell.RewardTrack == rewardTrack) == true;
        }

        private void CloseWindow()
        {
            UIManager.Hide<BattlePassWindowController>();
        }

        private void SubscribeTimer()
        {
            if (_battlePassTimerService == null)
            {
                return;
            }

            _battlePassTimerService.OnTimerUpdated -= HandleTimerUpdated;
            _battlePassTimerService.OnTimerUpdated += HandleTimerUpdated;
        }

        private void UnsubscribeTimer()
        {
            if (_battlePassTimerService == null)
            {
                return;
            }

            _battlePassTimerService.OnTimerUpdated -= HandleTimerUpdated;
        }

        private void ResetLoadCts()
        {
            CancelLoad();
            _loadCts = new CancellationTokenSource();
        }

        private void CancelLoad()
        {
            if (_loadCts == null)
            {
                return;
            }

            _loadCts.Cancel();
            _loadCts.Dispose();
            _loadCts = null;
        }
    }
}
