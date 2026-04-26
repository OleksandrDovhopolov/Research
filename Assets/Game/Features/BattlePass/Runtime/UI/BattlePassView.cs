using System;
using System.Collections.Generic;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public class BattlePassView : WindowView
    {
        private readonly List<BattlePassRewardView> _activeRewardViews = new();
        private bool _claimButtonsInteractable = true;

        [Header("State")]
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private GameObject _unavailableRoot;
        [SerializeField] private TMP_Text _unavailableText;

        [Header("Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _xpText;

        [Header("Buy Buttons")]
        [SerializeField] private Button _buyPremiumButton;
        [SerializeField] private Button _buyPlatinumButton;
        [SerializeField] private TMP_Text _buyPremiumLabel;
        [SerializeField] private TMP_Text _buyPlatinumLabel;

        [Header("Tracks")]
        [SerializeField] private UIListPool<BattlePassRewardView> _defaultRewardsPool;
        [SerializeField] private UIListPool<BattlePassRewardView> _premiumRewardsPool;

        public event Action BuyPremiumClick;
        public event Action BuyPlatinumClick;
        public event Action<int, BattlePassRewardTrack> RewardClaimClick;

        protected override void Awake()
        {
            base.Awake();

            if (_buyPremiumButton != null)
            {
                _buyPremiumButton.onClick.AddListener(HandleBuyPremiumClicked);
            }

            if (_buyPlatinumButton != null)
            {
                _buyPlatinumButton.onClick.AddListener(HandleBuyPlatinumClicked);
            }
        }

        public virtual void ResetView()
        {
            _claimButtonsInteractable = true;
            ClearRewardBindings();
            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(string.Empty);
            SetTimer(TimeSpan.Zero);
            SetLevel(0);
            SetXp(0, 0);
            SetBuyButtons(string.Empty, string.Empty);
            RenderRewards(_defaultRewardsPool, Array.Empty<BattlePassRewardUiModel>());
            RenderRewards(_premiumRewardsPool, Array.Empty<BattlePassRewardUiModel>());
        }

        public virtual void Render(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                return;
            }

            ClearRewardBindings();
            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(model.Title);
            SetLevel(model.CurrentLevel);
            SetXp(model.CurrentXp, model.RequiredXp);
            SetBuyButtons(model.PremiumProductId, model.PlatinumProductId);
            RenderRewards(_defaultRewardsPool, model.DefaultRewards);
            RenderRewards(_premiumRewardsPool, model.PremiumRewards);
        }

        public virtual void ShowUnavailableState(string message)
        {
            _claimButtonsInteractable = true;
            ClearRewardBindings();
            SetContentVisible(false);
            SetUnavailableVisible(true, string.IsNullOrWhiteSpace(message) ? BattlePassConfig.Ui.UnavailableText : message);
            RenderRewards(_defaultRewardsPool, Array.Empty<BattlePassRewardUiModel>());
            RenderRewards(_premiumRewardsPool, Array.Empty<BattlePassRewardUiModel>());
        }

        public virtual void SetClaimButtonsInteractable(bool isInteractable)
        {
            _claimButtonsInteractable = isInteractable;
            for (var i = 0; i < _activeRewardViews.Count; i++)
            {
                var rewardView = _activeRewardViews[i];
                if (rewardView == null)
                {
                    continue;
                }

                rewardView.SetClaimInteractable(isInteractable);
            }
        }

        public virtual void SetTimer(TimeSpan remainingTime)
        {
            if (_timerText != null)
            {
                _timerText.text = FormatTime(remainingTime);
            }
        }

        private void SetTitle(string title)
        {
            if (_titleText != null)
            {
                _titleText.text = title ?? string.Empty;
            }
        }

        private void SetLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = Mathf.Max(0, level).ToString();
            }
        }

        private void SetXp(int currentXp, int requiredXp)
        {
            if (_xpText != null)
            {
                var safeCurrentXp = Mathf.Max(0, currentXp);
                var safeRequiredXp = Mathf.Max(safeCurrentXp, requiredXp);
                _xpText.text = $"{safeCurrentXp} / {safeRequiredXp}";
            }
        }

        private void SetBuyButtons(string premiumProductId, string platinumProductId)
        {
            if (_buyPremiumLabel != null)
            {
                _buyPremiumLabel.text = premiumProductId ?? string.Empty;
            }

            if (_buyPlatinumLabel != null)
            {
                _buyPlatinumLabel.text = platinumProductId ?? string.Empty;
            }
        }

        private void RenderRewards(
            UIListPool<BattlePassRewardView> rewardsPool,
            IReadOnlyList<BattlePassRewardUiModel> rewards)
        {
            if (rewardsPool == null)
            {
                return;
            }

            rewardsPool.DisableAll();

            if (rewards == null)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                var rewardView = rewardsPool.GetNext();
                rewardView.SetData(reward);
                rewardView.SetClaimInteractable(_claimButtonsInteractable);
                rewardView.ClaimClick -= HandleRewardClaimClick;
                rewardView.ClaimClick += HandleRewardClaimClick;
                _activeRewardViews.Add(rewardView);
            }
        }

        private void SetContentVisible(bool isVisible)
        {
            if (_contentRoot != null)
            {
                _contentRoot.SetActive(isVisible);
            }
        }

        private void SetUnavailableVisible(bool isVisible, string message)
        {
            if (_unavailableRoot != null)
            {
                _unavailableRoot.SetActive(isVisible);
            }

            if (_unavailableText != null)
            {
                _unavailableText.text = message ?? string.Empty;
            }
        }

        private void HandleBuyPremiumClicked()
        {
            BuyPremiumClick?.Invoke();
        }

        private void HandleBuyPlatinumClicked()
        {
            BuyPlatinumClick?.Invoke();
        }

        protected void RaiseRewardClaimClick(int level, BattlePassRewardTrack rewardTrack)
        {
            RewardClaimClick?.Invoke(level, rewardTrack);
        }

        private void HandleRewardClaimClick(int level, BattlePassRewardTrack rewardTrack)
        {
            RaiseRewardClaimClick(level, rewardTrack);
        }

        private void ClearRewardBindings()
        {
            for (var i = 0; i < _activeRewardViews.Count; i++)
            {
                var rewardView = _activeRewardViews[i];
                if (rewardView == null)
                {
                    continue;
                }

                rewardView.ClaimClick -= HandleRewardClaimClick;
            }

            _activeRewardViews.Clear();
        }

        private static string FormatPassType(BattlePassPassType passType)
        {
            return passType switch
            {
                BattlePassPassType.None => "None",
                BattlePassPassType.Premium => "Premium",
                BattlePassPassType.Platinum => "Platinum",
                _ => "Unknown"
            };
        }

        private static string FormatTime(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            if (remaining.TotalDays >= 1)
            {
                return $"{remaining.Days}d {remaining.Hours}h";
            }

            return $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        protected override void OnDestroy()
        {
            ClearRewardBindings();

            if (_buyPremiumButton != null)
            {
                _buyPremiumButton.onClick.RemoveListener(HandleBuyPremiumClicked);
            }

            if (_buyPlatinumButton != null)
            {
                _buyPlatinumButton.onClick.RemoveListener(HandleBuyPlatinumClicked);
            }

            base.OnDestroy();
        }
    }
}
