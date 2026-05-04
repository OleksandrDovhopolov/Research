using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassSliderBarViewTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objectsToCleanup)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            _objectsToCleanup.Clear();
        }

        [Test]
        public void Render_CreatesMarkerForEachLevel()
        {
            var context = CreateContext(rewardsWidth: 800f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(new[] { 0, 25, 50, 75 }));

            Assert.That(context.LevelPool.ActiveElements().Count(), Is.EqualTo(4));
        }

        [Test]
        public void Render_LevelZeroShowsSpriteInsteadOfText()
        {
            var context = CreateContext(rewardsWidth: 800f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(new[] { 0, 25, 50 }));

            var firstMarker = context.LevelPool.ActiveElements().First();
            var zeroIcon = (Image)GetField(firstMarker, "_levelZeroIcon");
            var levelText = (TMP_Text)GetField(firstMarker, "_levelText");

            Assert.That(zeroIcon.gameObject.activeSelf, Is.True);
            Assert.That(zeroIcon.sprite, Is.SameAs(context.LevelZeroSprite));
            Assert.That(levelText.gameObject.activeSelf, Is.False);
            Assert.That(levelText.text, Is.Empty);
        }

        [Test]
        public void Render_SetsSliderWidthAndHalfCellInsets()
        {
            var context = CreateContext(rewardsWidth: 800f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(new[] { 0, 25, 50, 75 }));

            Assert.That(context.SliderBarContainer.sizeDelta.x, Is.EqualTo(800f).Within(0.001f));
            Assert.That(context.SliderBar.sizeDelta.x, Is.EqualTo(600f).Within(0.001f));
            Assert.That(context.SliderBar.anchoredPosition.x, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void Render_SetsProgressSlider_FromCumulativeSeasonXp()
        {
            var context = CreateContext(rewardsWidth: 800f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(
                new[] { 0, 25, 50, 100 },
                currentXp: 25,
                requiredXp: 50));

            Assert.That(context.ProgressSlider.value, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void Render_FallsBackToRequiredXp_WhenTotalThresholdIsUnavailable()
        {
            var context = CreateContext(rewardsWidth: 800f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(
                new[] { 0 },
                currentXp: 10,
                requiredXp: 40));

            Assert.That(context.ProgressSlider.value, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void Render_DistributesMarkersProportionallyAcrossSlider()
        {
            var context = CreateContext(rewardsWidth: 920f, rewardCellWidth: 120f);

            context.View.Render(CreateModel(new[] { 0, 25, 50, 75 }));

            var markerPositions = context.LevelPool.ActiveElements()
                .Select(view => view.RectTransform.anchoredPosition.x)
                .ToArray();

            Assert.That(markerPositions, Has.Length.EqualTo(4));
            Assert.That(markerPositions[0], Is.EqualTo(0f).Within(0.001f));
            Assert.That(markerPositions[1], Is.EqualTo(226.66667f).Within(0.001f));
            Assert.That(markerPositions[2], Is.EqualTo(453.33334f).Within(0.001f));
            Assert.That(markerPositions[3], Is.EqualTo(680f).Within(0.001f));
        }

        [Test]
        public void Render_DistributesMarkersProportionally_WithIrregularRewardLayout()
        {
            var context = CreateContext(
                rewardsWidth: 920f,
                rewardCellWidth: 120f,
                freeRewardLeftEdges: new[] { 0f, 800f },
                premiumRewardLeftEdges: new[] { 0f, 333f, 800f });

            context.View.Render(CreateModel(new[] { 0, 25, 50 }));

            var markerPositions = context.LevelPool.ActiveElements()
                .Select(view => view.RectTransform.anchoredPosition.x)
                .ToArray();

            Assert.That(markerPositions, Has.Length.EqualTo(3));
            Assert.That(markerPositions[0], Is.EqualTo(0f).Within(0.001f));
            Assert.That(markerPositions[1], Is.EqualTo(340f).Within(0.001f));
            Assert.That(markerPositions[2], Is.EqualTo(680f).Within(0.001f));
        }

        [Test]
        public void Render_WhenCountsRootIsOffset_UsesCountsLocalSpaceForMarkerPosition()
        {
            var context = CreateContext(
                rewardsWidth: 920f,
                rewardCellWidth: 120f,
                levelParentAnchoredX: -100f);

            context.View.Render(CreateModel(new[] { 0, 25, 50, 75 }));

            var markerPositions = context.LevelPool.ActiveElements()
                .Select(view => view.RectTransform.anchoredPosition.x)
                .ToArray();

            Assert.That(markerPositions, Has.Length.EqualTo(4));
            Assert.That(markerPositions[0], Is.EqualTo(100f).Within(0.001f));
            Assert.That(markerPositions[1], Is.EqualTo(326.66667f).Within(0.001f));
            Assert.That(markerPositions[2], Is.EqualTo(553.33334f).Within(0.001f));
            Assert.That(markerPositions[3], Is.EqualTo(780f).Within(0.001f));
        }

        [Test]
        public void Render_WhenNoLevels_ClearsPoolAndClampsWidthToNonNegative()
        {
            var context = CreateContext(rewardsWidth: 150f, rewardCellWidth: 200f);

            context.View.Render(CreateModel(Array.Empty<int>()));

            Assert.That(context.LevelPool.ActiveElements(), Is.Empty);
            Assert.That(context.SliderBar.sizeDelta.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(context.SliderBar.anchoredPosition.x, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void Render_UsesMaxWidthWhenRewardRowsDiffer()
        {
            var context = CreateContext(
                rewardsWidth: 600f,
                rewardCellWidth: 120f,
                premiumRewardsWidth: 800f,
                premiumRewardCellWidth: 200f);

            context.View.Render(CreateModel(new[] { 0, 25, 50 }));

            Assert.That(context.SliderBarContainer.sizeDelta.x, Is.EqualTo(800f).Within(0.001f));
            Assert.That(context.SliderBar.sizeDelta.x, Is.EqualTo(600f).Within(0.001f));
            Assert.That(context.SliderBar.anchoredPosition.x, Is.EqualTo(100f).Within(0.001f));
        }

        private TestContext CreateContext(
            float rewardsWidth,
            float rewardCellWidth,
            float? premiumRewardsWidth = null,
            float? premiumRewardCellWidth = null,
            IReadOnlyList<float> freeRewardLeftEdges = null,
            IReadOnlyList<float> premiumRewardLeftEdges = null,
            float levelParentAnchoredX = 0f)
        {
            var root = new GameObject("SliderBarViewRoot", typeof(RectTransform));
            _objectsToCleanup.Add(root);

            var sliderBarContainer = CreateRectTransform("SliderBarContainer", root.transform, width: 0f);
            var sliderBar = CreateRectTransform("SliderBar", sliderBarContainer, width: 0f);
            var progressSlider = CreateSlider("ProgressSlider", sliderBar);
            var levelParent = CreateRectTransform("LevelParent", sliderBar, width: 0f, anchoredX: levelParentAnchoredX);
            var freeRewardsRoot = CreateRewardRow(
                "FreeRewards",
                root.transform,
                rewardsWidth,
                rewardCellWidth,
                freeRewardLeftEdges);
            var premiumRewardsRoot = CreateRewardRow(
                "PremiumRewards",
                root.transform,
                premiumRewardsWidth ?? rewardsWidth,
                premiumRewardCellWidth ?? rewardCellWidth,
                premiumRewardLeftEdges);
            var levelZeroSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _objectsToCleanup.Add(levelZeroSprite);

            var levelPrefab = CreateLevelPrefab();
            var levelPool = new UIListPool<BattlePassSliderLevelView>(levelPrefab, levelParent, 0);
            var view = root.AddComponent<BattlePassSliderBarView>();

            SetField(view, "_sliderBarContainer", sliderBarContainer);
            SetField(view, "_sliderBar", sliderBar);
            SetField(view, "_progressSlider", progressSlider);
            SetField(view, "_measureFreeRewardsRoot", freeRewardsRoot);
            SetField(view, "_measurePremiumRewardsRoot", premiumRewardsRoot);
            SetField(view, "_levelPool", levelPool);
            SetField(view, "_levelZeroSprite", levelZeroSprite);

            return new TestContext(view, levelPool, sliderBarContainer, sliderBar, progressSlider, levelZeroSprite);
        }

        private GameObject CreateLevelPrefab()
        {
            var prefab = new GameObject("LevelPrefab", typeof(RectTransform), typeof(BattlePassSliderLevelView));
            _objectsToCleanup.Add(prefab);

            var textGo = new GameObject("LevelText", typeof(RectTransform));
            var iconGo = new GameObject("LevelIcon", typeof(RectTransform), typeof(Image));
            textGo.transform.SetParent(prefab.transform, false);
            iconGo.transform.SetParent(prefab.transform, false);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            var icon = iconGo.GetComponent<Image>();
            var view = prefab.GetComponent<BattlePassSliderLevelView>();
            SetField(view, "_levelText", text);
            SetField(view, "_levelZeroIcon", icon);
            prefab.SetActive(false);
            return prefab;
        }

        private RectTransform CreateRewardRow(
            string name,
            Transform parent,
            float width,
            float rewardCellWidth,
            IReadOnlyList<float> rewardLeftEdges = null)
        {
            var row = CreateRectTransform(name, parent, width);
            var leftEdges = rewardLeftEdges ?? new[] { 0f };
            for (var index = 0; index < leftEdges.Count; index++)
            {
                CreateRectTransform($"{name}_Reward_{index}", row, rewardCellWidth, leftEdges[index]);
            }

            return row;
        }

        private RectTransform CreateRectTransform(string name, Transform parent, float width, float anchoredX = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = new Vector2(width, 100f);
            rectTransform.anchoredPosition = new Vector2(anchoredX, 0f);
            return rectTransform;
        }

        private Slider CreateSlider(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private static BattlePassWindowUiModel CreateModel(
            IReadOnlyList<int> levelXpThresholds,
            int currentXp = 0,
            int requiredXp = 0)
        {
            return new BattlePassWindowUiModel(
                "Season 1",
                currentLevel: 0,
                currentXp: currentXp,
                requiredXp: requiredXp,
                levelXpThresholds,
                BattlePassPassType.None,
                string.Empty,
                string.Empty,
                Array.Empty<BattlePassRewardUiModel>(),
                Array.Empty<BattlePassRewardUiModel>());
        }

        private static object GetField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }

        private readonly struct TestContext
        {
            public TestContext(
                BattlePassSliderBarView view,
                UIListPool<BattlePassSliderLevelView> levelPool,
                RectTransform sliderBarContainer,
                RectTransform sliderBar,
                Slider progressSlider,
                Sprite levelZeroSprite)
            {
                View = view;
                LevelPool = levelPool;
                SliderBarContainer = sliderBarContainer;
                SliderBar = sliderBar;
                ProgressSlider = progressSlider;
                LevelZeroSprite = levelZeroSprite;
            }

            public BattlePassSliderBarView View { get; }
            public UIListPool<BattlePassSliderLevelView> LevelPool { get; }
            public RectTransform SliderBarContainer { get; }
            public RectTransform SliderBar { get; }
            public Slider ProgressSlider { get; }
            public Sprite LevelZeroSprite { get; }
        }
    }
}
