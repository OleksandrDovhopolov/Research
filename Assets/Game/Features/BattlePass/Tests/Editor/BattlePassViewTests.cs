using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassViewTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            
            DOTween.KillAll();

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
        public void BuildXpAnimationSteps_ReturnsPartialStep_ForLevelZeroProgress()
        {
            var steps = InvokeBuildXpAnimationSteps(0, 10, 0, 20, 30, new[] { 0, 30 });

            Assert.That(steps.Count, Is.EqualTo(1));
            Assert.That(GetStepProperty<int>(steps[0], "DisplayLevel"), Is.EqualTo(0));
            Assert.That(GetStepProperty<float>(steps[0], "TargetProgress"), Is.EqualTo(2f / 3f).Within(0.0001f));
            Assert.That(GetStepProperty<bool>(steps[0], "AdvancesLevel"), Is.False);
        }

        [Test]
        public void BuildXpAnimationSteps_ReturnsOnlyFullSteps_WhenCurrentLevelHasNoRemainder()
        {
            var steps = InvokeBuildXpAnimationSteps(1, 30, 2, 60, 90, new[] { 0, 30, 60, 90 });

            Assert.That(steps.Count, Is.EqualTo(1));
            Assert.That(GetStepProperty<int>(steps[0], "DisplayLevel"), Is.EqualTo(1));
            Assert.That(GetStepProperty<bool>(steps[0], "AdvancesLevel"), Is.True);
            Assert.That(GetStepProperty<int>(steps[0], "NextLevel"), Is.EqualTo(2));
        }

        [Test]
        public void BuildXpAnimationSteps_ReturnsLevelZeroToOneTransition()
        {
            var steps = InvokeBuildXpAnimationSteps(0, 50, 1, 120, 220, new[] { 0, 100, 220 });

            Assert.That(steps.Count, Is.EqualTo(2));
            Assert.That(GetStepProperty<int>(steps[0], "DisplayLevel"), Is.EqualTo(0));
            Assert.That(GetStepProperty<bool>(steps[0], "AdvancesLevel"), Is.True);
            Assert.That(GetStepProperty<int>(steps[0], "NextLevel"), Is.EqualTo(1));
            Assert.That(GetStepProperty<int>(steps[1], "DisplayLevel"), Is.EqualTo(1));
            Assert.That(GetStepProperty<float>(steps[1], "TargetProgress"), Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(GetStepProperty<bool>(steps[1], "AdvancesLevel"), Is.False);
        }

        [Test]
        public void BuildXpAnimationSteps_ReturnsFullAndPartialSteps_ForMultiLevelProgress()
        {
            var steps = InvokeBuildXpAnimationSteps(1, 45, 3, 100, 120, new[] { 0, 30, 60, 90, 120 });

            Assert.That(steps.Count, Is.EqualTo(3));
            Assert.That(GetStepProperty<int>(steps[0], "DisplayLevel"), Is.EqualTo(1));
            Assert.That(GetStepProperty<int>(steps[1], "DisplayLevel"), Is.EqualTo(2));
            Assert.That(GetStepProperty<int>(steps[2], "DisplayLevel"), Is.EqualTo(3));
            Assert.That(GetStepProperty<float>(steps[2], "TargetProgress"), Is.EqualTo(1f / 3f).Within(0.0001f));
            Assert.That(GetStepProperty<bool>(steps[2], "AdvancesLevel"), Is.False);
        }

        [Test]
        public void BuildXpAnimationSteps_ReturnsEmpty_WhenThresholdsAreIncomplete()
        {
            var steps = InvokeBuildXpAnimationSteps(2, 80, 3, 110, 150, new[] { 0, 30 });

            Assert.That(steps, Is.Empty);
        }

        [Test]
        public void BuildXpAnimationSteps_ReturnsEmpty_WhenProgressDidNotGrow()
        {
            var steps = InvokeBuildXpAnimationSteps(2, 75, 2, 75, 90, new[] { 0, 30, 60, 90 });

            Assert.That(steps, Is.Empty);
        }

        [Test]
        public void ResetView_KillsAnimation_ResetsSlider_AndRestoresInteraction()
        {
            var view = CreateRuntimeView(out var slider, out var canvasGroup);
            var model = CreateModel(currentLevel: 3, currentXp: 100, requiredXp: 120, levelXpThresholds: new[] { 0, 30, 60, 90, 120 });

            view.PrepareForOpenXpAnimation(1, 45, 3, 100, 120, new[] { 0, 30, 60, 90, 120 });
            view.Render(model);

            Assert.That(canvasGroup.interactable, Is.False);

            view.ResetView();

            Assert.That(slider.value, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);
        }

        [Test]
        public void Render_ClampsSliderValue_ToZeroOneRange()
        {
            var view = CreateRuntimeView(out var slider, out _);
            var model = CreateModel(currentLevel: 3, currentXp: 130, requiredXp: 120, levelXpThresholds: new[] { 0, 30, 60, 90, 120 });

            view.Render(model);

            Assert.That(slider.value, Is.InRange(0f, 1f));
        }

        private BattlePassView CreateRuntimeView(out Slider slider, out CanvasGroup canvasGroup)
        {
            var root = new GameObject("BattlePassViewRoot");
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));

            _objectsToCleanup.Add(root);

            sliderGo.transform.SetParent(root.transform, false);

            canvasGroup = root.AddComponent<CanvasGroup>();
            slider = sliderGo.GetComponent<Slider>();

            return root.AddComponent<BattlePassView>();
        }

        private static IList InvokeBuildXpAnimationSteps(
            int fromLevel,
            int fromXp,
            int toLevel,
            int toXp,
            int requiredXp,
            IReadOnlyList<int> thresholds)
        {
            var method = typeof(BattlePassView).GetMethod("BuildXpAnimationSteps", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            return (IList)method.Invoke(null, new object[] { fromLevel, fromXp, toLevel, toXp, requiredXp, thresholds });
        }

        private static T GetStepProperty<T>(object step, string propertyName)
        {
            var property = step.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Property '{propertyName}' was not found on step type '{step.GetType().Name}'.");
            return (T)property.GetValue(step);
        }

        private static BattlePassWindowUiModel CreateModel(
            int currentLevel,
            int currentXp,
            int requiredXp,
            IReadOnlyList<int> levelXpThresholds)
        {
            return new BattlePassWindowUiModel(
                "Season 1",
                currentLevel,
                currentXp,
                requiredXp,
                levelXpThresholds,
                BattlePassPassType.Premium,
                "premium_sku",
                "platinum_sku",
                Array.Empty<BattlePassRewardUiModel>(),
                Array.Empty<BattlePassRewardUiModel>());
        }
    }
}
