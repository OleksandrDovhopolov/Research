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
        private IBattlePassSnapshotStore _battlePassSnapshotStore;
        private IBattlePassXpPresentationTracker _xpPresentationTracker;
        private IBattlePassTimerService _battlePassTimerService;
        private IBattlePassRealtimeClock _realtimeClock;
        private IBattlePassOptimisticRewardApplier _optimisticRewardApplier;
        private BattlePassUiModelFactory _uiModelFactory;
        private IRewardPlayerStateRefreshCoordinator _rewardPlayerStateRefreshCoordinator;
        private IRewardSpecProvider _rewardSpecProvider;
        private Lock _claimFlowUiLock;

        private static readonly object ClaimFlowLockType = new();

        private CancellationTokenSource _loadCts;
        private BattlePassSnapshot _currentSnapshot;
        private bool _isClaimInFlight;

        [Inject]
        private void Construct(
            IBattlePassServerService battlePassServerService,
            IBattlePassSnapshotStore battlePassSnapshotStore,
            IBattlePassXpPresentationTracker xpPresentationTracker,
            IBattlePassTimerService battlePassTimerService,
            IBattlePassRealtimeClock realtimeClock,
            BattlePassUiModelFactory uiModelFactory,
            IBattlePassOptimisticRewardApplier optimisticRewardApplier,
            IRewardPlayerStateRefreshCoordinator rewardPlayerStateRefreshCoordinator,
            IRewardSpecProvider rewardSpecProvider)
        {
            _battlePassServerService = battlePassServerService;
            _battlePassSnapshotStore = battlePassSnapshotStore;
            _xpPresentationTracker = xpPresentationTracker;
            _battlePassTimerService = battlePassTimerService;
            _realtimeClock = realtimeClock;
            _uiModelFactory = uiModelFactory;
            _optimisticRewardApplier = optimisticRewardApplier;
            _rewardPlayerStateRefreshCoordinator = rewardPlayerStateRefreshCoordinator;
            _rewardSpecProvider = rewardSpecProvider;
        }

        protected override void OnShowStart()
        {
            ResetLoadCts();
            SubscribeTimer();
            SubscribeSnapshotStore();
            View.ShowLoadingState();

            if (TryApplySnapshotFromStore())
            {
                if (_battlePassSnapshotStore != null && _realtimeClock != null &&
                    _battlePassSnapshotStore.IsStale(_realtimeClock.UtcNow))
                {
                    RefreshSnapshotInBackground(_loadCts.Token);
                }

                return;
            }

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
            UnsubscribeSnapshotStore();
            UnsubscribeTimer();
            _battlePassTimerService?.Stop();
            _currentSnapshot = null;
            ReleaseClaimFlowLock();
            _isClaimInFlight = false;
        }

        private async UniTaskVoid LoadBattlePassAsync(CancellationToken ct)
        {
            try
            {
                await _battlePassSnapshotStore.RefreshAsync(ct, force: true);
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Failed to load Battle Pass data. {exception}");
                if (!TryApplySnapshotFromStore())
                {
                    View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                }
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
            AcquireClaimFlowLock();

            var claimResult = await _battlePassServerService.ClaimAsync(seasonId, level, rewardTrack, ct);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (claimResult is { Success: true })
                {
                    if (TryApplyUserState(claimResult.UpdatedUserState))
                    {
                        ReleaseClaimFlowLock();
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
                ReleaseClaimFlowLock();
                _isClaimInFlight = false;
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
                await _battlePassSnapshotStore.RefreshAsync(ct, force: true);
                ct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BattlePassWindowController] Failed to reload Battle Pass state after claim. {exception}");
                if (!TryApplySnapshotFromStore())
                {
                    View.ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                    _battlePassTimerService?.Stop();
                }
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
            var shouldCommitPresentedState = TryPrepareXpDeltaAnimation(snapshot, uiModel);

            View.Prewarm(uiModel);
            View.Render(uiModel);
            if (shouldCommitPresentedState)
            {
                CommitPresentedState(snapshot);
            }
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
            if (_battlePassSnapshotStore == null)
            {
                return false;
            }

            return _battlePassSnapshotStore.TryApplyUserState(updatedUserState);
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

        private void SubscribeSnapshotStore()
        {
            if (_battlePassSnapshotStore == null)
            {
                return;
            }

            _battlePassSnapshotStore.SnapshotChanged -= HandleSnapshotChanged;
            _battlePassSnapshotStore.SnapshotChanged += HandleSnapshotChanged;
        }

        private void UnsubscribeSnapshotStore()
        {
            if (_battlePassSnapshotStore == null)
            {
                return;
            }

            _battlePassSnapshotStore.SnapshotChanged -= HandleSnapshotChanged;
        }

        private void HandleSnapshotChanged(BattlePassSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            ApplySnapshot(snapshot);
        }

        private bool TryApplySnapshotFromStore()
        {
            var snapshot = _battlePassSnapshotStore?.CurrentSnapshot;
            if (snapshot == null)
            {
                return false;
            }

            ApplySnapshot(snapshot);
            return true;
        }

        private void RefreshSnapshotInBackground(CancellationToken ct)
        {
            RefreshSnapshotInBackgroundAsync(ct).Forget();
        }

        private async UniTaskVoid RefreshSnapshotInBackgroundAsync(CancellationToken ct)
        {
            try
            {
                await _battlePassSnapshotStore.RefreshAsync(ct, force: false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BattlePassWindowController] Background Battle Pass refresh failed. {exception.Message}");
            }
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

        private void AcquireClaimFlowLock()
        {
            if (_claimFlowUiLock != null || UIManager == null || UIManager.UiFilter == null)
            {
                return;
            }

            _claimFlowUiLock = UIManager.SetManualLock(ClaimFlowLockType);
        }

        private void ReleaseClaimFlowLock()
        {
            if (_claimFlowUiLock == null)
            {
                return;
            }

            _claimFlowUiLock.Dispose();
            _claimFlowUiLock = null;
        }

        private bool TryPrepareXpDeltaAnimation(BattlePassSnapshot snapshot, BattlePassWindowUiModel uiModel)
        {
            if (_xpPresentationTracker == null || snapshot?.Season == null || uiModel == null)
            {
                return false;
            }

            var seasonId = snapshot.Season.Id;
            if (string.IsNullOrWhiteSpace(seasonId))
            {
                return false;
            }

            var targetLevel = Mathf.Max(0, uiModel.CurrentLevel);
            var targetXp = Mathf.Max(0, uiModel.CurrentXp);

            if (!_xpPresentationTracker.TryGetBaseline(seasonId, out var fromLevel, out var fromXp))
            {
                _xpPresentationTracker.InitializeBaseline(seasonId, targetLevel, targetXp);
                return false;
            }

            if (!HasProgressGrowth(fromLevel, fromXp, targetLevel, targetXp))
            {
                _xpPresentationTracker.CommitPresented(seasonId, targetLevel, targetXp);
                return false;
            }

            View.PrepareForOpenXpAnimation(
                fromLevel,
                fromXp,
                targetLevel,
                targetXp,
                uiModel.RequiredXp,
                uiModel.LevelXpThresholds);

            return true;
        }

        private void CommitPresentedState(BattlePassSnapshot snapshot)
        {
            if (_xpPresentationTracker == null || snapshot?.Season == null || snapshot.UserState == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(snapshot.Season.Id))
            {
                return;
            }

            _xpPresentationTracker.CommitPresented(
                snapshot.Season.Id,
                snapshot.UserState.Level,
                snapshot.UserState.Xp);
        }

        private static bool HasProgressGrowth(int fromLevel, int fromXp, int toLevel, int toXp)
        {
            var safeFromLevel = Mathf.Max(0, fromLevel);
            var safeFromXp = Mathf.Max(0, fromXp);
            var safeToLevel = Mathf.Max(0, toLevel);
            var safeToXp = Mathf.Max(0, toXp);

            return safeToLevel > safeFromLevel ||
                   (safeToLevel == safeFromLevel && safeToXp > safeFromXp);
        }
    }
}
