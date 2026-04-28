using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public class BattlePassView : WindowView
    {
        private readonly struct XpAnimationStep
        {
            public XpAnimationStep(int displayLevel, float targetProgress, bool advancesLevel, int nextLevel)
            {
                DisplayLevel = Mathf.Max(0, displayLevel);
                TargetProgress = Mathf.Clamp01(targetProgress);
                AdvancesLevel = advancesLevel;
                NextLevel = Mathf.Max(0, nextLevel);
            }

            public int DisplayLevel { get; }
            public float TargetProgress { get; }
            public bool AdvancesLevel { get; }
            public int NextLevel { get; }
        }

        private readonly List<BattlePassRewardView> _activeRewardViews = new();
        private Sequence _xpAnimationSequence;
        private bool _claimButtonsInteractable = true;
        private bool _shouldAnimateXpOnNextRender;
        private int _animationFromLevel;
        private int _animationFromXp;
        private int _animationToLevel;
        private int _animationToXp;
        private int _animationRequiredXp;
        private IReadOnlyList<int> _animationLevelXpThresholds = Array.Empty<int>();

        [Header("State")]
        [SerializeField] private GameObject _contentRoot;
        [SerializeField] private GameObject _unavailableRoot;
        [SerializeField] private TMP_Text _unavailableText;

        [Header("Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private Slider _xpSlider;
        [SerializeField] private CanvasGroup _rootCanvasGroup;

        [Header("Buy Buttons")]
        [SerializeField] private Button _buyPremiumButton;
        [SerializeField] private Button _buyPlatinumButton;
        [SerializeField] private TMP_Text _buyPremiumLabel;
        [SerializeField] private TMP_Text _buyPlatinumLabel;

        [Header("XP Animation")]
        [SerializeField] private float _fullLevelFillDuration = 0.2f;
        [SerializeField] private float _currentLevelFillDuration = 0.3f;

        [Header("Tracks")]
        [SerializeField] private UIListPool<BattlePassRewardView> _defaultRewardsPool;
        [SerializeField] private UIListPool<BattlePassRewardView> _premiumRewardsPool;

        public event Action BuyPremiumClick;
        public event Action BuyPlatinumClick;
        public event Action<int, BattlePassRewardTrack> RewardClaimClick;

        protected override void Awake()
        {
            base.Awake();
            TryResolveProgressComponents();

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
            StopXpAnimation();
            _shouldAnimateXpOnNextRender = false;
            ClearPendingXpAnimationState();
            _claimButtonsInteractable = true;
            ClearRewardBindings();
            SetWindowInteraction(true);
            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(string.Empty);
            SetTimer(TimeSpan.Zero);
            SetLevel(0);
            SetXpText(0, 0);
            SetXpProgress(0f);
            SetBuyButtons(string.Empty, string.Empty);
            RenderRewards(_defaultRewardsPool, Array.Empty<BattlePassRewardUiModel>());
            RenderRewards(_premiumRewardsPool, Array.Empty<BattlePassRewardUiModel>());
        }

        public virtual void PrepareForOpenXpAnimation(
            int fromLevel,
            int fromXp,
            int toLevel,
            int toXp,
            int requiredXp,
            IReadOnlyList<int> levelXpThresholds)
        {
            _animationFromLevel = Mathf.Max(0, fromLevel);
            _animationFromXp = Mathf.Max(0, fromXp);
            _animationToLevel = Mathf.Max(0, toLevel);
            _animationToXp = Mathf.Max(0, toXp);
            _animationRequiredXp = Mathf.Max(0, requiredXp);
            _animationLevelXpThresholds = levelXpThresholds ?? Array.Empty<int>();
            _shouldAnimateXpOnNextRender = true;
        }

        public virtual void ShowLoadingState()
        {
            ResetView();
            SetClaimButtonsInteractable(false);
        }

        public virtual void Prewarm(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                return;
            }

            PrewarmRewards(_defaultRewardsPool, model.DefaultRewards);
            PrewarmRewards(_premiumRewardsPool, model.PremiumRewards);
        }

        public virtual void Render(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                ShowUnavailableState(BattlePassConfig.Ui.UnavailableText);
                return;
            }

            StopXpAnimation();
            TryResolveProgressComponents();
            ClearRewardBindings();
            SetWindowInteraction(true);
            SetContentVisible(true);
            SetUnavailableVisible(false, string.Empty);
            SetTitle(model.Title);
            SetXpText(model.CurrentXp, model.RequiredXp);
            SetBuyButtons(model.PremiumProductId, model.PlatinumProductId);
            SetClaimButtonsInteractable(true);
            RenderRewards(_defaultRewardsPool, model.DefaultRewards);
            RenderRewards(_premiumRewardsPool, model.PremiumRewards);

            if (_shouldAnimateXpOnNextRender && TryStartOpenXpAnimation())
            {
                _shouldAnimateXpOnNextRender = false;
                ClearPendingXpAnimationState();
                return;
            }

            _shouldAnimateXpOnNextRender = false;
            ClearPendingXpAnimationState();
            SetLevel(model.CurrentLevel);
            SetXpProgress(ResolveNormalizedXpProgress(
                model.CurrentXp,
                model.RequiredXp,
                model.CurrentLevel,
                model.LevelXpThresholds));
        }

        public virtual void ShowUnavailableState(string message)
        {
            StopXpAnimation();
            _shouldAnimateXpOnNextRender = false;
            ClearPendingXpAnimationState();
            _claimButtonsInteractable = true;
            ClearRewardBindings();
            SetWindowInteraction(true);
            SetContentVisible(false);
            SetUnavailableVisible(true, string.IsNullOrWhiteSpace(message) ? BattlePassConfig.Ui.UnavailableText : message);
            SetLevel(0);
            SetXpText(0, 0);
            SetXpProgress(0f);
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

        private bool TryStartOpenXpAnimation()
        {
            if (_xpSlider == null)
            {
                return false;
            }

            var steps = BuildXpAnimationSteps(
                _animationFromLevel,
                _animationFromXp,
                _animationToLevel,
                _animationToXp,
                _animationRequiredXp,
                _animationLevelXpThresholds);
            if (steps.Count == 0)
            {
                return false;
            }

            var fromLevel = Mathf.Max(0, _animationFromLevel);
            var fromProgress = ResolveNormalizedXpProgress(
                _animationFromXp,
                _animationRequiredXp,
                fromLevel,
                _animationLevelXpThresholds);
            SetWindowInteraction(false);
            SetLevel(steps[0].DisplayLevel);
            SetXpProgress(fromProgress);

            var finalLevel = Mathf.Max(0, _animationToLevel);
            var finalProgress = ResolveNormalizedXpProgress(
                _animationToXp,
                _animationRequiredXp,
                _animationToLevel,
                _animationLevelXpThresholds);

            _xpAnimationSequence = DOTween.Sequence()
                .SetId(this)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SetLevel(finalLevel);
                    SetXpProgress(finalProgress);
                    SetWindowInteraction(true);
                    _xpAnimationSequence = null;
                })
                .OnKill(() => _xpAnimationSequence = null);

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                _xpAnimationSequence.Append(DOTween
                    .To(() => _xpSlider.value, value => _xpSlider.value = value, step.TargetProgress, ResolveStepDuration(step))
                    .SetEase(Ease.OutCubic));

                if (!step.AdvancesLevel)
                {
                    continue;
                }

                _xpAnimationSequence.AppendCallback(() =>
                {
                    SetLevel(step.NextLevel);
                    SetXpProgress(0f);
                });
            }

            return true;
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

        private void SetXpText(int currentXp, int requiredXp)
        {
            if (_xpText != null)
            {
                var safeCurrentXp = Mathf.Max(0, currentXp);
                var safeRequiredXp = Mathf.Max(safeCurrentXp, requiredXp);
                _xpText.text = $"{safeCurrentXp} / {safeRequiredXp}";
            }
        }

        private void SetXpProgress(float normalizedProgress)
        {
            if (_xpSlider == null)
            {
                return;
            }

            _xpSlider.minValue = 0f;
            _xpSlider.maxValue = 1f;
            _xpSlider.wholeNumbers = false;
            _xpSlider.value = Mathf.Clamp01(normalizedProgress);
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

        private static void PrewarmRewards(
            UIListPool<BattlePassRewardView> rewardsPool,
            IReadOnlyList<BattlePassRewardUiModel> rewards)
        {
            if (rewardsPool == null || rewards == null)
            {
                return;
            }

            rewardsPool.CheckSize(rewards.Count);
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

        private void SetWindowInteraction(bool isInteractable)
        {
            if (_rootCanvasGroup == null)
            {
                return;
            }

            _rootCanvasGroup.interactable = isInteractable;
            _rootCanvasGroup.blocksRaycasts = true;
        }

        private void TryResolveProgressComponents()
        {
            if (_xpSlider == null)
            {
                _xpSlider = GetComponentInChildren<Slider>(true);
            }

            if (_rootCanvasGroup == null)
            {
                _rootCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void StopXpAnimation()
        {
            if (_xpAnimationSequence == null)
            {
                return;
            }

            _xpAnimationSequence.Kill();
            _xpAnimationSequence = null;
        }

        private void ClearPendingXpAnimationState()
        {
            _animationFromLevel = 0;
            _animationFromXp = 0;
            _animationToLevel = 0;
            _animationToXp = 0;
            _animationRequiredXp = 0;
            _animationLevelXpThresholds = Array.Empty<int>();
        }

        private float ResolveStepDuration(XpAnimationStep step)
        {
            return step.AdvancesLevel
                ? Mathf.Max(0.01f, _fullLevelFillDuration)
                : Mathf.Max(0.01f, _currentLevelFillDuration);
        }

        private static List<XpAnimationStep> BuildXpAnimationSteps(
            int fromLevel,
            int fromXp,
            int toLevel,
            int toXp,
            int requiredXp,
            IReadOnlyList<int> levelXpThresholds)
        {
            var steps = new List<XpAnimationStep>();
            var safeFromLevel = Mathf.Max(0, fromLevel);
            var safeToLevel = Mathf.Max(0, toLevel);
            var safeFromXp = Mathf.Max(0, fromXp);
            var safeToXp = Mathf.Max(0, toXp);

            if (!HasProgressGrowth(safeFromLevel, safeFromXp, safeToLevel, safeToXp))
            {
                return steps;
            }

            if (!TryGetLevelStartXp(safeFromLevel, levelXpThresholds, out _))
            {
                return steps;
            }

            if (!TryGetLevelStartXp(safeToLevel, levelXpThresholds, out _))
            {
                return steps;
            }

            var fromProgress = ResolveNormalizedXpProgress(safeFromXp, requiredXp, safeFromLevel, levelXpThresholds);
            var toProgress = ResolveNormalizedXpProgress(safeToXp, requiredXp, safeToLevel, levelXpThresholds);

            if (safeFromLevel == safeToLevel)
            {
                if (toProgress > fromProgress)
                {
                    steps.Add(new XpAnimationStep(safeFromLevel, toProgress, advancesLevel: false, nextLevel: safeFromLevel));
                }

                return steps;
            }

            if (fromProgress < 1f)
            {
                steps.Add(new XpAnimationStep(safeFromLevel, 1f, advancesLevel: true, nextLevel: safeFromLevel + 1));
            }

            for (var displayLevel = safeFromLevel + 1; displayLevel < safeToLevel; displayLevel++)
            {
                steps.Add(new XpAnimationStep(displayLevel, 1f, advancesLevel: true, nextLevel: displayLevel + 1));
            }

            if (toProgress > 0f)
            {
                steps.Add(new XpAnimationStep(safeToLevel, toProgress, advancesLevel: false, nextLevel: safeToLevel));
            }

            return steps;
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

        private static float ResolveNormalizedXpProgress(
            int currentXp,
            int requiredXp,
            int currentLevel,
            IReadOnlyList<int> levelXpThresholds)
        {
            var safeCurrentXp = Mathf.Max(0, currentXp);
            var safeRequiredXp = Mathf.Max(safeCurrentXp, requiredXp);
            if (safeRequiredXp <= 0)
            {
                return 0f;
            }

            if (!TryGetLevelStartXp(currentLevel, levelXpThresholds, out var currentLevelStartXp))
            {
                return Mathf.Clamp01((float)safeCurrentXp / safeRequiredXp);
            }

            var currentLevelEndXp = ResolveLevelEndXp(
                currentLevel,
                currentLevelStartXp,
                safeRequiredXp,
                levelXpThresholds,
                safeCurrentXp);
            var segmentSize = currentLevelEndXp - currentLevelStartXp;
            if (segmentSize <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(safeCurrentXp - currentLevelStartXp) / segmentSize);
        }

        private static int ResolveLevelEndXp(
            int level,
            int currentLevelStartXp,
            int fallbackRequiredXp,
            IReadOnlyList<int> levelXpThresholds,
            int currentXp)
        {
            if (TryGetLevelStartXp(level + 1, levelXpThresholds, out var nextLevelStartXp))
            {
                return Mathf.Max(currentLevelStartXp, nextLevelStartXp);
            }

            return Mathf.Max(currentLevelStartXp, Mathf.Max(fallbackRequiredXp, currentXp));
        }

        private static bool TryGetLevelStartXp(int level, IReadOnlyList<int> levelXpThresholds, out int levelStartXp)
        {
            if (level < 0)
            {
                levelStartXp = 0;
                return false;
            }

            if (levelXpThresholds == null || levelXpThresholds.Count == 0)
            {
                levelStartXp = 0;
                return level == 0;
            }

            if (level >= levelXpThresholds.Count)
            {
                levelStartXp = 0;
                return false;
            }

            levelStartXp = Mathf.Max(0, levelXpThresholds[level]);
            return true;
        }

        private void HandleBuyPremiumClicked()
        {
            RaiseBuyPremiumClick();
        }

        private void HandleBuyPlatinumClicked()
        {
            RaiseBuyPlatinumClick();
        }

        protected void RaiseBuyPremiumClick()
        {
            BuyPremiumClick?.Invoke();
        }

        protected void RaiseBuyPlatinumClick()
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
            StopXpAnimation();
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
