using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewards;
using UISystem;
using UnityEngine;
using VContainer;

namespace FortuneWheel
{
    public sealed class FortuneWheelSectorArgs
    {
        public string RewardId { get; }
        public Sprite RewardIcon { get; }
        public int RewardAmount { get; }

        public FortuneWheelSectorArgs(string rewardId, Sprite rewardIcon, int rewardAmount)
        {
            RewardId = rewardId;
            RewardIcon = rewardIcon;
            RewardAmount = rewardAmount;
        }
    }

    public class FortuneWheelArgs : WindowArgs
    {
        public const int SectorCount = 8;

        public int SpinsAmount { get; }
        public TimeSpan RemainingTime { get; }
        public IReadOnlyList<FortuneWheelSectorArgs> Sectors { get; }

        public FortuneWheelArgs(int spinsAmount, TimeSpan remainingTime, IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            SpinsAmount = spinsAmount;
            RemainingTime = remainingTime;
            Sectors = sectors;
        }
    }

    [Window("FortuneWheelWindow")]
    public class FortuneWheelController : WindowController<FortuneWheelView>
    {
        private IRewardSpecProvider _rewardSpecProvider;
        private IFortuneWheelServerService _fortuneWheelServerService;

        private FortuneWheelArgs Args => Arguments as FortuneWheelArgs;

        private int _currentSpinsAmount;
        private bool _isSpinning;
        private bool _isSpinRequestInProgress;
        private bool _isDataValid;
        private CancellationTokenSource _requestCts;
        private FortuneWheelSpinResult _pendingSpinResult;
        private TimeSpan _currentRemainingTime;
        private IReadOnlyList<FortuneWheelSectorArgs> _currentSectors = Array.Empty<FortuneWheelSectorArgs>();

        public override bool IsCloseBlocked => _isSpinning;

        [Inject]
        private void Construct(IFortuneWheelServerService fortuneWheelServerService, IRewardSpecProvider rewardSpecProvider)
        {
            _rewardSpecProvider = rewardSpecProvider;
            _fortuneWheelServerService = fortuneWheelServerService;
        }

        protected override void OnShowStart()
        {
            ResetRequestCts();

            var args = Args;
            if (args == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(FortuneWheelArgs)} are missing.");
                _isDataValid = false;
                _currentSpinsAmount = 0;
                _currentSectors = Array.Empty<FortuneWheelSectorArgs>();
                _currentRemainingTime = TimeSpan.Zero;
                UpdateInteractionState();
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, args.SpinsAmount);
            _currentRemainingTime = args.RemainingTime;
            _currentSectors = CloneSectors(args.Sectors);
            _isDataValid = ValidateSectors(_currentSectors);

            RefreshViewData();
            UpdateInteractionState();

            LoadRewardsFromServerAsync(_requestCts.Token).Forget();
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.SpinClick += OnSpinClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.SpinClick -= OnSpinClicked;
            View.CloseClick -= CloseWindow;
            CancelRequests();
            CancelSpin();
        }

        private void OnSpinClicked()
        {
            OnSpinClickedAsync(_requestCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid LoadRewardsFromServerAsync(CancellationToken ct)
        {
            if (_fortuneWheelServerService == null)
            {
                return;
            }

            try
            {
                var rewardsFromServer = await _fortuneWheelServerService.GetRewardsAsync(ct);
                var sectors = BuildSectorsFromServerRewards(rewardsFromServer);
                if (!ValidateSectors(sectors))
                {
                    Debug.LogWarning($"[{nameof(FortuneWheelController)}] Invalid sectors from server. Keeping fallback args data.");
                    return;
                }

                _currentSectors = sectors;
                _isDataValid = true;
                RefreshViewData();
                UpdateInteractionState();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Failed to load wheel rewards from server. {exception.Message}");
            }
        }

        private async UniTaskVoid OnSpinClickedAsync(CancellationToken ct)
        {
            if (_isSpinning || _isSpinRequestInProgress || !_isDataValid || _currentSpinsAmount <= 0)
            {
                return;
            }

            if (_fortuneWheelServerService == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(IFortuneWheelServerService)} is not available.");
                return;
            }

            _isSpinRequestInProgress = true;
            UpdateInteractionState();

            try
            {
                var spinResult = await _fortuneWheelServerService.SpinAsync(ct);
                var targetSectorIndex = FindSectorIndexByRewardId(_currentSectors, spinResult.RewardId);

                if (targetSectorIndex < 0)
                {
                    Debug.LogWarning($"[{nameof(FortuneWheelController)}] Reward id '{spinResult.RewardId}' was not found in sectors.");
                    return;
                }

                _pendingSpinResult = spinResult;
                _isSpinning = true;
                UpdateInteractionState();

                var animationStarted = View.PlaySpinToSector(targetSectorIndex, OnSpinCompleted);
                if (!animationStarted)
                {
                    _isSpinning = false;
                    ApplySpinResult(_pendingSpinResult);
                    _pendingSpinResult = null;
                    UpdateInteractionState();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Spin request failed. {exception.Message}");
            }
            finally
            {
                _isSpinRequestInProgress = false;
                if (!_isSpinning)
                {
                    UpdateInteractionState();
                }
            }
        }

        private void OnSpinCompleted()
        {
            if (!_isSpinning)
            {
                return;
            }

            _isSpinning = false;

            if (_pendingSpinResult != null)
            {
                ApplySpinResult(_pendingSpinResult);
                _pendingSpinResult = null;
            }

            UpdateInteractionState();
        }

        private void UpdateInteractionState()
        {
            View.SetSpinInteractable(_isDataValid && !_isSpinning && !_isSpinRequestInProgress && _currentSpinsAmount > 0);
            View.SetCloseInteractable(!_isSpinning);
        }

        private void CancelSpin()
        {
            _isSpinning = false;
            _isSpinRequestInProgress = false;
            _pendingSpinResult = null;
            View.StopSpinAnimation();
            UpdateInteractionState();
        }

        private void CancelRequests()
        {
            if (_requestCts == null)
            {
                return;
            }

            _requestCts.Cancel();
            _requestCts.Dispose();
            _requestCts = null;
        }

        private void ResetRequestCts()
        {
            CancelRequests();
            _requestCts = new CancellationTokenSource();
        }

        private void RefreshViewData()
        {
            View.SetData(new FortuneWheelArgs(_currentSpinsAmount, _currentRemainingTime, _currentSectors));
        }

        private void ApplySpinResult(FortuneWheelSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, spinResult.AvailableSpins);
            _currentRemainingTime = TimeSpan.FromSeconds(Mathf.Max(0, spinResult.NextRegenSeconds));
            RefreshViewData();
        }

        private IReadOnlyList<FortuneWheelSectorArgs> BuildSectorsFromServerRewards(IReadOnlyList<FortuneWheelRewardServerItem> rewardsFromServer)
        {
            if (rewardsFromServer == null || rewardsFromServer.Count != FortuneWheelArgs.SectorCount)
            {
                return Array.Empty<FortuneWheelSectorArgs>();
            }

            var existingById = _currentSectors
                .Where(sector => sector != null && !string.IsNullOrWhiteSpace(sector.RewardId))
                .GroupBy(sector => sector.RewardId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var sectors = new FortuneWheelSectorArgs[FortuneWheelArgs.SectorCount];
            for (var i = 0; i < rewardsFromServer.Count; i++)
            {
                var rewardId = rewardsFromServer[i]?.RewardId;
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    return Array.Empty<FortuneWheelSectorArgs>();
                }

                var icon = default(Sprite);
                var amount = 0;

                if (_rewardSpecProvider != null && _rewardSpecProvider.TryGet(rewardId, out var rewardSpec))
                {
                    icon = rewardSpec.Icon;
                    amount = rewardSpec.TotalAmountForUi;
                }
                else if (existingById.TryGetValue(rewardId, out var fallbackSector))
                {
                    icon = fallbackSector.RewardIcon;
                    amount = fallbackSector.RewardAmount;
                }

                sectors[i] = new FortuneWheelSectorArgs(rewardId, icon, amount);
            }

            return sectors;
        }

        private static int FindSectorIndexByRewardId(IReadOnlyList<FortuneWheelSectorArgs> sectors, string rewardId)
        {
            if (sectors == null || string.IsNullOrWhiteSpace(rewardId))
            {
                return -1;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                if (sector != null && string.Equals(sector.RewardId, rewardId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ValidateSectors(IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            if (sectors == null || sectors.Count != FortuneWheelArgs.SectorCount)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] Wheel expects exactly {FortuneWheelArgs.SectorCount} sectors. Now is {sectors.Count}");
                return false;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                if (sectors[i] == null || string.IsNullOrWhiteSpace(sectors[i].RewardId))
                {
                    Debug.LogError($"[{nameof(FortuneWheelController)}] Sector {i} is null.");
                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<FortuneWheelSectorArgs> CloneSectors(IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            if (sectors == null)
            {
                return Array.Empty<FortuneWheelSectorArgs>();
            }

            var result = new FortuneWheelSectorArgs[sectors.Count];
            for (var i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                result[i] = sector == null
                    ? null
                    : new FortuneWheelSectorArgs(sector.RewardId, sector.RewardIcon, sector.RewardAmount);
            }

            return result;
        }

        private void CloseWindow()
        {
            if (_isSpinning)
            {
                return;
            }

            UIManager.Hide<FortuneWheelController>();
        }
    }
}
