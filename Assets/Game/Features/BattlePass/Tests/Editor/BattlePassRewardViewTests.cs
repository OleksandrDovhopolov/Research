using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassRewardViewTests
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
        public void SetData_WhenRewardIsClaimed_ShowsClaimedOverlay()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot);
            var reward = new BattlePassRewardUiModel("reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, true, false, false);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.True);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void SetData_WhenPremiumRewardIsLocked_ShowsLockedOverlay()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot);
            var reward = new BattlePassRewardUiModel("reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, false, true, true);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.False);
            Assert.That(lockedStateRoot.activeSelf, Is.True);
        }

        [Test]
        public void SetData_WhenDefaultRewardIsMarkedLocked_DoesNotShowLockedOverlay()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out var lockedStateRoot);
            var reward = new BattlePassRewardUiModel("reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, true, false);

            rewardView.SetData(reward);

            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void Cleanup_HidesClaimedAndLockedOverlays()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot);
            var reward = new BattlePassRewardUiModel("reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, true, true, true);
            rewardView.SetData(reward);

            rewardView.Cleanup();

            Assert.That(claimedStateRoot.activeSelf, Is.False);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        private BattlePassRewardView CreateRewardView(
            out Image iconImage,
            out TMPro.TMP_Text amountText,
            out GameObject claimedStateRoot,
            out GameObject lockedStateRoot)
        {
            var root = new GameObject("BattlePassRewardView");
            var iconGo = new GameObject("Icon");
            var claimedGo = new GameObject("Claimed");
            var lockedGo = new GameObject("Locked");

            iconGo.transform.SetParent(root.transform);
            claimedGo.transform.SetParent(root.transform);
            lockedGo.transform.SetParent(root.transform);

            _objectsToCleanup.Add(root);

            iconImage = iconGo.AddComponent<Image>();
            amountText = root.AddComponent<TMPro.TextMeshProUGUI>();
            claimedStateRoot = claimedGo;
            lockedStateRoot = lockedGo;

            var rewardView = root.AddComponent<BattlePassRewardView>();
            SetField(rewardView, "_iconImage", iconImage);
            SetField(rewardView, "_amountText", amountText);
            SetField(rewardView, "_claimedStateRoot", claimedStateRoot);
            SetField(rewardView, "_lockedStateRoot", lockedStateRoot);

            claimedStateRoot.SetActive(false);
            lockedStateRoot.SetActive(false);

            return rewardView;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }
    }
}
