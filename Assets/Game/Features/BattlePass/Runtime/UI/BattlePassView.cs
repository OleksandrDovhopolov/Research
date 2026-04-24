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
        [Header("State")]
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private GameObject _unavailableRoot;
        [SerializeField] private TMP_Text _unavailableText;

        [Header("Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private TMP_Text _passTypeText;

        [Header("Buy Buttons")]
        [SerializeField] private Button _buyPremiumButton;
        [SerializeField] private Button _buyPlatinumButton;
        [SerializeField] private TMP_Text _buyPremiumLabel;
        [SerializeField] private TMP_Text _buyPlatinumLabel;

        [Header("Tracks")]
        [SerializeField] private UIListPool<BattlePassRewardTrackItemView> _defaultRewardsPool;
        [SerializeField] private UIListPool<BattlePassRewardTrackItemView> _premiumRewardsPool;

        public event Action BuyPremiumClick;
        public event Action BuyPlatinumClick;

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
            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(string.Empty);
            SetTimer(TimeSpan.Zero);
            SetLevel(0);
            SetXp(0);
            SetPassType(BattlePassPassType.Unknown);
            SetBuyButtons(string.Empty, string.Empty);
            RenderTrack(_defaultRewardsPool, Array.Empty<BattlePassTrackLevelUiModel>());
            RenderTrack(_premiumRewardsPool, Array.Empty<BattlePassTrackLevelUiModel>());
        }

        public virtual void Render(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                return;
            }

            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(model.Title);
            SetLevel(model.CurrentLevel);
            SetXp(model.CurrentXp);
            SetPassType(model.PassType);
            SetBuyButtons(model.PremiumProductId, model.PlatinumProductId);
            RenderTrack(_defaultRewardsPool, model.DefaultTrackLevels);
            RenderTrack(_premiumRewardsPool, model.PremiumTrackLevels);
        }

        public virtual void ShowUnavailableState(string message)
        {
            SetContentVisible(false);
            SetUnavailableVisible(true, string.IsNullOrWhiteSpace(message) ? BattlePassConfig.Ui.UnavailableText : message);
            RenderTrack(_defaultRewardsPool, Array.Empty<BattlePassTrackLevelUiModel>());
            RenderTrack(_premiumRewardsPool, Array.Empty<BattlePassTrackLevelUiModel>());
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

        private void SetXp(int xp)
        {
            if (_xpText != null)
            {
                _xpText.text = Mathf.Max(0, xp).ToString();
            }
        }

        private void SetPassType(BattlePassPassType passType)
        {
            if (_passTypeText != null)
            {
                _passTypeText.text = FormatPassType(passType);
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

        private void RenderTrack(
            UIListPool<BattlePassRewardTrackItemView> trackPool,
            IReadOnlyList<BattlePassTrackLevelUiModel> levels)
        {
            if (trackPool == null)
            {
                return;
            }

            trackPool.DisableAll();

            if (levels == null)
            {
                return;
            }

            foreach (var level in levels)
            {
                var trackItemView = trackPool.GetNext();
                trackItemView.SetData(level);
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
