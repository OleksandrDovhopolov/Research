using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(1, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, true, false, false, false);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.True);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void SetData_WhenPremiumRewardIsLocked_ShowsLockedOverlay()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(1, BattlePassRewardTrack.Premium, "reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, false, false, true, true);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.False);
            Assert.That(lockedStateRoot.activeSelf, Is.True);
        }

        [Test]
        public void SetData_WhenDefaultRewardIsMarkedLocked_DoesNotShowLockedOverlay()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(1, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, false, true, false);

            rewardView.SetData(reward);

            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void SetData_WhenRewardIsClaimed_DoesNotShowLockedOverlayEvenIfLocked()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(1, BattlePassRewardTrack.Premium, "reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, true, false, true, true);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.True);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void Cleanup_HidesClaimedAndLockedOverlays()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(1, BattlePassRewardTrack.Premium, "reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, true, false, true, true);
            rewardView.SetData(reward);

            rewardView.Cleanup();

            Assert.That(claimedStateRoot.activeSelf, Is.False);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        [Test]
        public void SetData_WhenRewardIsClaimable_ShowsClaimButton()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out var claimButtonRoot, out var claimButton, out _);
            var reward = new BattlePassRewardUiModel(2, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, true, false, false);

            rewardView.SetData(reward);

            Assert.That(claimButtonRoot.activeSelf, Is.True);
            Assert.That(claimButton.gameObject.activeSelf, Is.True);
            Assert.That(claimButton.interactable, Is.True);
        }

        [Test]
        public void SetData_WhenRewardIsNotClaimable_HidesClaimButton()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out var claimButtonRoot, out var claimButton, out _);
            var reward = new BattlePassRewardUiModel(2, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, false, false, false);

            rewardView.SetData(reward);

            Assert.That(claimButtonRoot.activeSelf, Is.False);
            Assert.That(claimButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SetData_WhenRewardIsRegular_EnablesRewardButton()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out _, out _, out var rewardButton);
            var reward = new BattlePassRewardUiModel(2, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, false, false, false);

            rewardView.SetData(reward);

            Assert.That(rewardButton.enabled, Is.True);
            Assert.That(rewardButton.interactable, Is.True);
        }

        [Test]
        public void ClaimButtonClick_RaisesClaimEventWithLevelAndTrack()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out _, out var claimButton, out _);
            var reward = new BattlePassRewardUiModel(3, BattlePassRewardTrack.Premium, "reward_premium", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 25, false, true, false, true);
            rewardView.SetData(reward);

            var claimRaised = false;
            var raisedLevel = 0;
            var raisedTrack = BattlePassRewardTrack.Default;
            rewardView.ClaimClick += (level, track) =>
            {
                claimRaised = true;
                raisedLevel = level;
                raisedTrack = track;
            };

            claimButton.onClick.Invoke();

            Assert.That(claimRaised, Is.True);
            Assert.That(raisedLevel, Is.EqualTo(3));
            Assert.That(raisedTrack, Is.EqualTo(BattlePassRewardTrack.Premium));
        }

        [Test]
        public void SetData_WhenPremiumLevelZeroPlaceholder_ShowsCtaAndClearsAmountText()
        {
            var rewardView = CreateRewardView(out _, out var amountText, out _, out _, out var claimButtonRoot, out var claimButton, out var placeholderCtaButton);
            var reward = new BattlePassRewardUiModel(
                0,
                BattlePassRewardTrack.Premium,
                string.Empty,
                null,
                0,
                false,
                false,
                true,
                true,
                isPremiumPlaceholderLevelZero: true);

            rewardView.SetData(reward);

            Assert.That(amountText.text, Is.Empty);
            Assert.That(claimButtonRoot.activeSelf, Is.False);
            Assert.That(claimButton.gameObject.activeSelf, Is.False);
            Assert.That(placeholderCtaButton.enabled, Is.True);
            Assert.That(placeholderCtaButton.interactable, Is.True);
        }

        [Test]
        public void PremiumPlaceholderCtaClick_RaisesEvent()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out _, out _, out var rewardButton);
            var reward = new BattlePassRewardUiModel(
                0,
                BattlePassRewardTrack.Premium,
                string.Empty,
                null,
                0,
                false,
                false,
                true,
                true,
                isPremiumPlaceholderLevelZero: true);
            rewardView.SetData(reward);

            var raised = false;
            rewardView.PremiumPlaceholderCtaClick += () => raised = true;

            rewardButton.onClick.Invoke();

            Assert.That(raised, Is.True);
        }

        [Test]
        public void RewardButtonClick_WhenRewardIsRegular_LogsRewardId_AndDoesNotRaisePremiumPlaceholderEvent()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out _, out _, out var rewardButton);
            var reward = new BattlePassRewardUiModel(
                2,
                BattlePassRewardTrack.Default,
                "reward_default",
                Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero),
                10,
                false,
                false,
                false,
                false);
            rewardView.SetData(reward);

            var raised = false;
            rewardView.PremiumPlaceholderCtaClick += () => raised = true;
            LogAssert.Expect(LogType.Log, "[BattlePassRewardView] Reward button clicked. RewardId='reward_default'");

            rewardButton.onClick.Invoke();

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Cleanup_DisablesRewardButton()
        {
            var rewardView = CreateRewardView(out _, out _, out _, out _, out _, out _, out var rewardButton);
            var reward = new BattlePassRewardUiModel(2, BattlePassRewardTrack.Default, "reward_default", Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero), 10, false, false, false, false);
            rewardView.SetData(reward);

            rewardView.Cleanup();

            Assert.That(rewardButton.enabled, Is.False);
            Assert.That(rewardButton.interactable, Is.False);
        }

        [Test]
        public void SetData_WhenPremiumLevelZeroPlaceholder_AlwaysHidesClaimedAndLockedStates()
        {
            var rewardView = CreateRewardView(out _, out _, out var claimedStateRoot, out var lockedStateRoot, out _, out _, out _);
            var reward = new BattlePassRewardUiModel(
                0,
                BattlePassRewardTrack.Premium,
                string.Empty,
                null,
                0,
                isClaimed: true,
                isClaimable: false,
                isLocked: true,
                isPremiumTrack: true,
                isPremiumPlaceholderLevelZero: true);

            rewardView.SetData(reward);

            Assert.That(claimedStateRoot.activeSelf, Is.False);
            Assert.That(lockedStateRoot.activeSelf, Is.False);
        }

        private BattlePassRewardView CreateRewardView(
            out Image iconImage,
            out TMPro.TMP_Text amountText,
            out GameObject claimedStateRoot,
            out GameObject lockedStateRoot,
            out GameObject claimButtonRoot,
            out Button claimButton,
            out Button rewardButton)
        {
            var root = new GameObject("BattlePassRewardView");
            var iconGo = new GameObject("Icon");
            var claimedGo = new GameObject("Claimed");
            var lockedGo = new GameObject("Locked");
            var claimButtonRootGo = new GameObject("ClaimButtonRoot");
            var claimButtonGo = new GameObject("ClaimButton");
            var rewardButtonGo = new GameObject("RewardButton");

            iconGo.transform.SetParent(root.transform);
            claimedGo.transform.SetParent(root.transform);
            lockedGo.transform.SetParent(root.transform);
            claimButtonRootGo.transform.SetParent(root.transform);
            claimButtonGo.transform.SetParent(claimButtonRootGo.transform);
            rewardButtonGo.transform.SetParent(root.transform);

            _objectsToCleanup.Add(root);

            iconImage = iconGo.AddComponent<Image>();
            amountText = root.AddComponent<TMPro.TextMeshProUGUI>();
            claimedStateRoot = claimedGo;
            lockedStateRoot = lockedGo;
            claimButtonRoot = claimButtonRootGo;
            claimButton = claimButtonGo.AddComponent<Button>();
            rewardButton = rewardButtonGo.AddComponent<Button>();

            var rewardView = root.AddComponent<BattlePassRewardView>();
            SetField(rewardView, "_iconImage", iconImage);
            SetField(rewardView, "_amountText", amountText);
            SetField(rewardView, "_claimedStateRoot", claimedStateRoot);
            SetField(rewardView, "_lockedStateRoot", lockedStateRoot);
            SetField(rewardView, "_claimButtonRoot", claimButtonRoot);
            SetField(rewardView, "_claimButton", claimButton);
            SetField(rewardView, "bpRewardButton", rewardButton);

            claimedStateRoot.SetActive(false);
            lockedStateRoot.SetActive(false);
            claimButtonRoot.SetActive(false);
            claimButton.gameObject.SetActive(false);
            rewardButton.enabled = false;
            rewardButton.interactable = false;

            InvokeMethod(rewardView, "Awake");

            return rewardView;
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Method '{methodName}' was not found.");
            method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }
    }
}
