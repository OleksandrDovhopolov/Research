using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UISystem;
using UnityEngine;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassIAPWindowControllerTests
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
        public void GeneratePurchaseToken_UsesMockPremiumPrefix_AndIsUnique()
        {
            var controller = new TokenGenerationBattlePassIAPWindowController();

            var token1 = controller.CreateToken();
            var token2 = controller.CreateToken();

            Assert.That(token1, Does.StartWith("mock_premium_"));
            Assert.That(token2, Does.StartWith("mock_premium_"));
            Assert.That(token1, Is.Not.EqualTo(token2));
        }

        [Test]
        public void Purchase_WhenSuccessful_VerifiesToken_ClosesPopup_AndReportsResult()
        {
            var expectedResult = new BattlePassPurchaseVerificationResult(
                true,
                "acknowledged",
                CreatePremiumUserState(),
                "battle_pass",
                "premium_sku",
                "active",
                null,
                null);
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => UniTask.FromResult(expectedResult)
            };
            BattlePassPurchaseVerificationResult callbackResult = null;
            var controller = CreateController(
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", result => callbackResult = result),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(1));
            Assert.That(serverService.LastSeasonId, Is.EqualTo("season_1"));
            Assert.That(serverService.LastProductId, Is.EqualTo("premium_sku"));
            Assert.That(serverService.LastPurchaseToken, Is.EqualTo("mock_premium_testtoken"));
            Assert.That(callbackResult, Is.SameAs(expectedResult));
            Assert.That(controller.CloseCalls, Is.EqualTo(1));
            Assert.That(controller.InfoMessages, Has.Member("Battle Pass premium purchase completed successfully."));
            Assert.That(view.LastStatus, Is.EqualTo("Purchase completed successfully."));
        }

        [Test]
        public void Purchase_WhenGrantedWithSuccessFalse_ClosesPopup_AndReportsResult()
        {
            var expectedResult = new BattlePassPurchaseVerificationResult(
                false,
                "granted",
                CreatePremiumUserState(),
                "battle_pass",
                "premium_sku",
                "active",
                "PURCHASE_ACKNOWLEDGE_FAILED",
                "Stub acknowledge failed.");
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => UniTask.FromResult(expectedResult)
            };
            BattlePassPurchaseVerificationResult callbackResult = null;
            var controller = CreateController(
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", result => callbackResult = result),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(callbackResult, Is.SameAs(expectedResult));
            Assert.That(controller.CloseCalls, Is.EqualTo(1));
            Assert.That(controller.InfoMessages, Has.Member("Stub acknowledge failed."));
            Assert.That(view.LastStatus, Is.EqualTo("Stub acknowledge failed."));
        }

        [Test]
        public void Purchase_WhenPending_DoesNotClosePopup_AndShowsProcessingStatus()
        {
            var pendingVerification = new UniTaskCompletionSource<BattlePassPurchaseVerificationResult>();
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => pendingVerification.Task
            };
            var controller = CreateController(
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();
            view.EmitPurchaseClick();

            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(1));
            Assert.That(view.LastPurchaseButtonInteractable, Is.False);

            pendingVerification.TrySetResult(new BattlePassPurchaseVerificationResult(
                false,
                "pending",
                null,
                null,
                null,
                null,
                "PURCHASE_NOT_PURCHASED",
                "Provider purchase state is 'pending'."));

            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages, Has.Member("Purchase is processing."));
            Assert.That(view.LastStatus, Is.EqualTo("Purchase is processing."));
            Assert.That(view.LastPurchaseButtonInteractable, Is.True);
        }

        [Test]
        public void Purchase_WhenVerificationFails_KeepsPopupOpen_AndShowsError()
        {
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => UniTask.FromResult(new BattlePassPurchaseVerificationResult(
                    false,
                    "failed",
                    null,
                    null,
                    null,
                    null,
                    "PURCHASE_VERIFY_FAILED",
                    "Stub verifier failed the purchase token."))
            };
            var controller = CreateController(
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages, Has.Member("Stub verifier failed the purchase token."));
            Assert.That(view.LastStatus, Is.EqualTo("Stub verifier failed the purchase token."));
            Assert.That(view.LastPurchaseButtonInteractable, Is.True);
        }

        private TestBattlePassIAPWindowController CreateController(
            StubBattlePassServerService serverService,
            BattlePassIAPWindowArgs args,
            out TestBattlePassIAPWindowView view)
        {
            var uiManagerGo = new GameObject("BattlePassIAPUIManager");
            var viewGo = new GameObject("BattlePassIAPView");

            _objectsToCleanup.Add(uiManagerGo);
            _objectsToCleanup.Add(viewGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            view = viewGo.AddComponent<TestBattlePassIAPWindowView>();

            var controller = new TestBattlePassIAPWindowController(args);
            controller.Configurate(view, uiManager, new WindowAttribute("BattlePassPremiumWindow", WindowType.Popup));
            controller.SetEventHandler(new StubEventHandler());

            var constructMethod = typeof(BattlePassIAPWindowController).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(controller, new object[] { serverService });

            return controller;
        }

        private static BattlePassUserState CreatePremiumUserState()
        {
            return new BattlePassUserState(
                "season_1",
                1,
                0,
                BattlePassPassType.Premium,
                Array.Empty<BattlePassClaimedRewardCell>(),
                Array.Empty<BattlePassClaimableRewardCell>());
        }

        private static void RunCoroutine(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
            }
        }

        private sealed class TestBattlePassIAPWindowView : BattlePassIAPWindowView
        {
            public string LastStatus { get; private set; }
            public bool LastPurchaseButtonInteractable { get; private set; } = true;

            public override void ResetView()
            {
                LastStatus = null;
                LastPurchaseButtonInteractable = true;
            }

            public override void SetStatus(string status)
            {
                LastStatus = status;
            }

            public override void SetPurchaseButtonInteractable(bool isInteractable)
            {
                LastPurchaseButtonInteractable = isInteractable;
            }

            public void EmitPurchaseClick()
            {
                RaisePurchaseClick();
            }
        }

        private sealed class TestBattlePassIAPWindowController : BattlePassIAPWindowController
        {
            public TestBattlePassIAPWindowController(BattlePassIAPWindowArgs args)
            {
                TestArgs = args;
            }

            public BattlePassIAPWindowArgs TestArgs { get; }
            public int CloseCalls { get; private set; }
            public List<string> InfoMessages { get; } = new();

            protected override string GeneratePurchaseToken()
            {
                return "mock_premium_testtoken";
            }

            protected override void ShowInfo(string message)
            {
                InfoMessages.Add(message);
            }

            protected override void CloseWindow()
            {
                CloseCalls++;
            }
        }

        private sealed class TokenGenerationBattlePassIAPWindowController : BattlePassIAPWindowController
        {
            public string CreateToken()
            {
                return base.GeneratePurchaseToken();
            }
        }

        private sealed class StubBattlePassServerService : IBattlePassServerService
        {
            public int VerifyPurchaseCalls { get; private set; }
            public string LastSeasonId { get; private set; }
            public string LastProductId { get; private set; }
            public string LastPurchaseToken { get; private set; }
            public Func<string, string, string, UniTask<BattlePassPurchaseVerificationResult>> VerifyPurchaseResponseFactory { get; set; }

            public UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                throw new NotImplementedException();
            }

            public UniTask<BattlePassAddXpResult> AddXpAsync(int amount, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                throw new NotImplementedException();
            }

            public UniTask<BattlePassClaimResult> ClaimAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                throw new NotImplementedException();
            }

            public UniTask<BattlePassPurchaseVerificationResult> VerifyGooglePurchaseAsync(
                string seasonId,
                string productId,
                string purchaseToken,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                VerifyPurchaseCalls++;
                LastSeasonId = seasonId;
                LastProductId = productId;
                LastPurchaseToken = purchaseToken;

                if (VerifyPurchaseResponseFactory != null)
                {
                    return VerifyPurchaseResponseFactory(seasonId, productId, purchaseToken);
                }

                return UniTask.FromResult(new BattlePassPurchaseVerificationResult(
                    true,
                    "acknowledged",
                    CreatePremiumUserState(),
                    "battle_pass",
                    productId,
                    "active",
                    null,
                    null));
            }
        }

        //TODO fix this class 
        private sealed class StubEventHandler : UIManagerEventHandlerBase
        {
            public override void WindowShowEventInvoke(IWindowController window)
            {
                throw new NotImplementedException();
            }

            public override void WindowHideEventInvoke(IWindowController window, bool isClosed)
            {
                throw new NotImplementedException();
            }

            public override void WindowAnimationEventInvoke(IWindowController window, WindowAnimationType eventType)
            {
                throw new NotImplementedException();
            }

            public override void StackCommandProcessedEventInvoke(UICommand uiCommand)
            {
                throw new NotImplementedException();
            }

            public override void StackCommandProcessEventInvoke(UICommand uiCommand)
            {
                throw new NotImplementedException();
            }

            public override void StackCommandProcessAddEventInvoke(UICommand uiCommand)
            {
                throw new NotImplementedException();
            }
        }
    }
}
