using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewards;
using UIShared;
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
        private IBattlePassOptimisticRewardApplier _optimisticRewardApplier;
        private BattlePassUiModelFactory _uiModelFactory;
        private IRewardPlayerStateRefreshCoordinator _rewardPlayerStateRefreshCoordinator;
        private IRewardSpecProvider _rewardSpecProvider;

        private CancellationTokenSource _loadCts;
        private BattlePassSnapshot _currentSnapshot;
        private bool _isClaimInFlight;

        [Inject]
        private void Construct(
            IBattlePassServerService battlePassServerService,
            IBattlePassTimerService battlePassTimerService,
            BattlePassUiModelFactory uiModelFactory,
            IBattlePassOptimisticRewardApplier optimisticRewardApplier,
            IRewardPlayerStateRefreshCoordinator rewardPlayerStateRefreshCoordinator,
            IRewardSpecProvider rewardSpecProvider)
        {
            _battlePassServerService = battlePassServerService;
            _battlePassTimerService = battlePassTimerService;
            _uiModelFactory = uiModelFactory;
            _optimisticRewardApplier = optimisticRewardApplier;
            _rewardPlayerStateRefreshCoordinator = rewardPlayerStateRefreshCoordinator;
            _rewardSpecProvider = rewardSpecProvider;
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
            var seasonId = _currentSnapshot?.Season?.Id;
            var productId = _currentSnapshot?.Products?.PremiumProductId;

            if (string.IsNullOrWhiteSpace(seasonId) || string.IsNullOrWhiteSpace(productId))
            {
                ShowInfo("Battle Pass premium purchase is unavailable. Missing seasonId or productId.");
                return;
            }

            if (HasPremiumAccess(_currentSnapshot?.UserState?.PassType ?? BattlePassPassType.Unknown))
            {
                ShowInfo("Battle Pass premium is already active.");
                return;
            }

            ShowPremiumPurchaseWindow(new BattlePassIAPWindowArgs(seasonId, productId, HandleBattlePassPurchaseVerified));
        }

        private void HandleBuyPlatinumClicked()
        {
            ShowInfo("Battle Pass platinum purchase is not supported in the mock client.");
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

            var claimResult = await _battlePassServerService.ClaimAsync(seasonId, level, rewardTrack, ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (claimResult is { Success: true })
                {
                    if (TryApplyUserState(claimResult.UpdatedUserState))
                    {
                        _optimisticRewardApplier?.Apply(claimResult.GrantedRewards);
                        TryShowRewardWindow(claimResult.GrantedRewards);
                        _rewardPlayerStateRefreshCoordinator?.RequestBackgroundRefresh();
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

        private void TryShowRewardWindow(System.Collections.Generic.IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards)
        {
            var rewardId = grantedRewards?.FirstOrDefault()?.RewardId;
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                Debug.LogWarning("[BattlePassWindowController] Claim succeeded, but grantedRewards is empty.");
                return;
            }

            if (_rewardSpecProvider != null && !_rewardSpecProvider.TryGet(rewardId, out _))
            {
                return;
            }

            var rewardArgs = new RewardsWindowArgs(rewardId);
            UIManager.Show<RewardsWindowController>(rewardArgs);
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

        private void HandleBattlePassPurchaseVerified(BattlePassPurchaseVerificationResult result)
        {
            if (result?.UpdatedUserState == null)
            {
                return;
            }

            if (TryApplyUserState(result.UpdatedUserState))
            {
                return;
            }

            Debug.LogError("[BattlePassWindowController] Purchase verification returned Battle Pass state, but it could not be merged.");
            ReloadCurrentAsync(_loadCts?.Token ?? CancellationToken.None).Forget();
        }

        private bool TryApplyUserState(BattlePassUserState updatedUserState)
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

        protected virtual void ShowInfo(string message)
        {
            UIManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        protected virtual void ShowPremiumPurchaseWindow(BattlePassIAPWindowArgs args)
        {
            UIManager.Show<BattlePassIAPWindowController>(args);
        }

        private static bool HasPremiumAccess(BattlePassPassType passType)
        {
            return passType is BattlePassPassType.Premium or BattlePassPassType.Platinum;
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
