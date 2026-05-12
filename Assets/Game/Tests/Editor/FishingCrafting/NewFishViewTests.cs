using System.Collections.Generic;
using System.Reflection;
using Game.Fishing;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class NewFishViewTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _objectsToCleanup.Count - 1; i >= 0; i--)
            {
                if (_objectsToCleanup[i] != null)
                    Object.DestroyImmediate(_objectsToCleanup[i]);
            }

            _objectsToCleanup.Clear();
        }

        [Test]
        public void Render_UpdatesWeights_NewBadge_AndUnlockedStars()
        {
            var root = new GameObject("NewFishViewRoot");
            var view = root.AddComponent<NewFishView>();
            var caughtWeightText = CreateText("CaughtWeight");
            var bestWeightText = CreateText("BestWeight");
            var newBadge = CreateGameObject("IsNewBadge");
            var commonState = CreateGameObject("CommonState");
            var rareState = CreateGameObject("RareState");
            var epicState = CreateGameObject("EpicState");
            var legendaryState = CreateGameObject("LegendaryState");
            var fishCardGo = new GameObject("FishCard");
            var fishCardView = fishCardGo.AddComponent<FishCollectionItemView>();
            fishCardGo.transform.SetParent(root.transform, false);

            _objectsToCleanup.Add(root);
            _objectsToCleanup.Add(fishCardGo);

            SetField(view, "_caughtWeightText", caughtWeightText);
            SetField(view, "_isNewFishGameObject", newBadge);
            SetField(fishCardView, "_bestCaughtWeightText", bestWeightText);
            SetField(fishCardView, "_commonCollectedObject", commonState);
            SetField(fishCardView, "_rareCollectedObject", rareState);
            SetField(fishCardView, "_epicCollectedObject", epicState);
            SetField(fishCardView, "_legendaryCollectedObject", legendaryState);

            view.Render(new NewFishArgs(
                string.Empty,
                true,
                2.5f,
                3.75f,
                true,
                new[] { "common", "legendary" }));

            Assert.That(caughtWeightText.text, Is.EqualTo("2.50"));
            Assert.That(bestWeightText.text, Is.EqualTo("3.75"));
            Assert.That(newBadge.activeSelf, Is.True);
            Assert.That(commonState.activeSelf, Is.True);
            Assert.That(rareState.activeSelf, Is.False);
            Assert.That(epicState.activeSelf, Is.False);
            Assert.That(legendaryState.activeSelf, Is.True);
        }

        private TextMeshProUGUI CreateText(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            _objectsToCleanup.Add(gameObject);
            return text;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _objectsToCleanup.Add(gameObject);
            return gameObject;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}
