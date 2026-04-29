using System;
using System.Collections.Generic;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass
{
    public class BattlePassSliderBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _sliderBarContainer;
        [SerializeField] private RectTransform _sliderBar;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private RectTransform _measureFreeRewardsRoot;
        [SerializeField] private RectTransform _measurePremiumRewardsRoot;
        [SerializeField] private UIListPool<BattlePassSliderLevelView> _levelPool;
        [SerializeField] private Sprite _levelZeroSprite;

        public virtual void ResetView()
        {
            _levelPool?.DisableAll();
            SetWidth(_sliderBarContainer, 0f);
            SetWidth(_sliderBar, 0f);
            SetAnchoredX(_sliderBar, 0f);
            SetProgress(0f);
        }

        public virtual void Prewarm(BattlePassWindowUiModel model)
        {
            if (_levelPool == null || model == null)
            {
                return;
            }

            _levelPool.CheckSize(Mathf.Max(0, model.LevelXpThresholds?.Count ?? 0));
        }

        public virtual void ForceRebuildLayout()
        {
            ForceRebuild(_measurePremiumRewardsRoot);
            ForceRebuild(_measureFreeRewardsRoot);
            ForceRebuild(_sliderBarContainer);
            ForceRebuild(_sliderBar);
        }

        public virtual void Render(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                ResetView();
                return;
            }

            _levelPool?.DisableAll();

            var levelCount = Mathf.Max(0, model.LevelXpThresholds?.Count ?? 0);
            var rewardsWidth = ResolveRewardsWidth();
            var rewardCellWidth = ResolveRewardCellWidth();
            var leftInset = rewardCellWidth > 0f ? rewardCellWidth * 0.5f : 0f;
            var rightInset = rewardCellWidth > 0f ? rewardCellWidth * 0.5f : 0f;
            var sliderWidth = Mathf.Max(0f, rewardsWidth - leftInset - rightInset);

            SetWidth(_sliderBarContainer, rewardsWidth);
            SetWidth(_sliderBar, sliderWidth);
            SetAnchoredX(_sliderBar, leftInset);
            SetProgress(ResolveCumulativeProgress(model));

            if (_levelPool == null || levelCount <= 0)
            {
                return;
            }

            for (var level = 0; level < levelCount; level++)
            {
                var levelView = _levelPool.GetNext();
                levelView.SetLevel(level, _levelZeroSprite);
                levelView.SetAnchoredX(ResolveMarkerX(level, levelCount));
            }
        }

        private float ResolveRewardsWidth()
        {
            var freeWidth = GetWidth(_measureFreeRewardsRoot);
            var premiumWidth = GetWidth(_measurePremiumRewardsRoot);

            if (freeWidth > 0f && premiumWidth > 0f && !Mathf.Approximately(freeWidth, premiumWidth))
            {
                Debug.LogWarning($"[BattlePassSliderBarView] Reward row widths differ. Free={freeWidth}, Premium={premiumWidth}. Using the larger width.");
            }

            return Mathf.Max(freeWidth, premiumWidth);
        }

        private float ResolveRewardCellWidth()
        {
            return Mathf.Max(
                ResolveFirstRewardCellWidth(_measureFreeRewardsRoot),
                ResolveFirstRewardCellWidth(_measurePremiumRewardsRoot));
        }

        private void SetProgress(float normalizedProgress)
        {
            if (_progressSlider == null)
            {
                return;
            }

            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = 1f;
            _progressSlider.wholeNumbers = false;
            _progressSlider.value = Mathf.Clamp01(normalizedProgress);
        }

        private static float ResolveCumulativeProgress(BattlePassWindowUiModel model)
        {
            if (model == null)
            {
                return 0f;
            }

            var safeCurrentXp = Mathf.Max(0, model.CurrentXp);
            var totalRequiredXp = ResolveTotalRequiredXp(model, safeCurrentXp);
            if (totalRequiredXp <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)safeCurrentXp / totalRequiredXp);
        }

        private static int ResolveTotalRequiredXp(BattlePassWindowUiModel model, int safeCurrentXp)
        {
            if (model?.LevelXpThresholds != null && model.LevelXpThresholds.Count > 0)
            {
                var lastThreshold = Mathf.Max(0, model.LevelXpThresholds[model.LevelXpThresholds.Count - 1]);
                if (lastThreshold > 0)
                {
                    return Mathf.Max(lastThreshold, safeCurrentXp);
                }
            }

            return Mathf.Max(0, Mathf.Max(model?.RequiredXp ?? 0, safeCurrentXp));
        }

        private float ResolveMarkerX(int level, int levelCount)
        {
            if (_sliderBar == null || _levelPool?.Parent is not RectTransform levelMarkersRoot)
            {
                return 0f;
            }

            if (levelCount <= 1)
            {
                return ResolveSliderLocalX(levelMarkersRoot, 0f);
            }

            var normalized = Mathf.Clamp01(level / (levelCount - 1f));
            return ResolveSliderLocalX(levelMarkersRoot, normalized);
        }

        private float ResolveSliderLocalX(RectTransform levelMarkersRoot, float normalized)
        {
            var clampedNormalized = Mathf.Clamp01(normalized);
            var sliderLocalX = Mathf.Lerp(_sliderBar.rect.xMin, _sliderBar.rect.xMax, clampedNormalized);
            var sliderWorldPoint = _sliderBar.TransformPoint(new Vector3(sliderLocalX, 0f, 0f));
            var localPoint = levelMarkersRoot.InverseTransformPoint(sliderWorldPoint);
            return localPoint.x;
        }

        private static float ResolveFirstRewardCellWidth(RectTransform root)
        {
            if (root == null)
            {
                return 0f;
            }

            RectTransform fallbackChild = null;
            for (var i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i) is not RectTransform child)
                {
                    continue;
                }

                fallbackChild ??= child;
                if (child.gameObject.activeSelf)
                {
                    return GetWidth(child);
                }
            }

            return GetWidth(fallbackChild);
        }

        private static void ForceRebuild(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private static float GetWidth(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return 0f;
            }

            var width = rectTransform.rect.width;
            if (width > 0f)
            {
                return width;
            }

            return Mathf.Max(0f, rectTransform.sizeDelta.x);
        }

        private static void SetWidth(RectTransform rectTransform, float width)
        {
            if (rectTransform == null)
            {
                return;
            }

            var sizeDelta = rectTransform.sizeDelta;
            sizeDelta.x = Mathf.Max(0f, width);
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void SetAnchoredX(RectTransform rectTransform, float anchoredX)
        {
            if (rectTransform == null)
            {
                return;
            }

            var anchoredPosition = rectTransform.anchoredPosition;
            anchoredPosition.x = anchoredX;
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
