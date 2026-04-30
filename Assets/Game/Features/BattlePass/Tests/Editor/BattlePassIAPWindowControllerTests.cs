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
        public void MockPurchaseService_UsesMockPremiumPrefix()
        {
            var service = new MockBattlePassPurchaseService();
            var result = service.PurchaseAsync("premium_sku").GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(BattlePassStorePurchaseStatus.Succeeded));
            Assert.That(result.PurchaseToken, Does.StartWith("mock_premium_"));
        }

        [Test]
        public void Purchase_WhenSuccessful_VerifiesToken_ConsumesPurchase_ClosesPopup_AndReportsResult()
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
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Succeeded,
                    "google_token_123",
                    "txn_1",
                    "premium_sku",
                    string.Empty)),
                ConsumeResponseFactory = (_, _) => UniTask.FromResult(new BattlePassConsumeResult(
                    BattlePassConsumeStatus.Succeeded,
                    string.Empty))
            };
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => UniTask.FromResult(expectedResult)
            };
            BattlePassPurchaseVerificationResult callbackResult = null;
            var controller = CreateController(
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", result => callbackResult = result),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(purchaseService.PurchaseCalls, Is.EqualTo(1));
            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(1));
            Assert.That(serverService.LastPurchaseToken, Is.EqualTo("google_token_123"));
            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(1));
            Assert.That(purchaseService.LastConsumedToken, Is.EqualTo("google_token_123"));
            Assert.That(callbackResult, Is.SameAs(expectedResult));
            Assert.That(controller.CloseCalls, Is.EqualTo(1));
            Assert.That(controller.InfoMessages, Has.Member("Battle Pass premium purchase completed successfully."));
            Assert.That(view.LastStatus, Is.EqualTo("Purchase completed successfully."));
        }

        [Test]
        public void Purchase_WhenStoreReturnsPending_DoesNotVerifyOrConsume_AndShowsProcessingStatus()
        {
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Pending,
                    string.Empty,
                    string.Empty,
                    "premium_sku",
                    string.Empty))
            };
            var serverService = new StubBattlePassServerService();
            var controller = CreateController(
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(0));
            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(0));
            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages, Has.Member("Purchase is processing."));
            Assert.That(view.LastStatus, Is.EqualTo("Purchase is processing."));
            Assert.That(view.LastPurchaseButtonInteractable, Is.True);
        }

        [Test]
        public void Purchase_WhenStoreIsCancelled_DoesNotVerifyOrConsume_AndKeepsPopupOpen()
        {
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Cancelled,
                    string.Empty,
                    string.Empty,
                    "premium_sku",
                    "Purchase was cancelled."))
            };
            var serverService = new StubBattlePassServerService();
            var controller = CreateController(
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(0));
            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(0));
            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(view.LastStatus, Is.EqualTo("Purchase was cancelled."));
            Assert.That(view.LastPurchaseButtonInteractable, Is.True);
        }

        [Test]
        public void Purchase_WhenStoreFails_DoesNotVerifyOrConsume_AndShowsError()
        {
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Failed,
                    string.Empty,
                    string.Empty,
                    "premium_sku",
                    "Store purchase failed."))
            };
            var serverService = new StubBattlePassServerService();
            var controller = CreateController(
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(serverService.VerifyPurchaseCalls, Is.EqualTo(0));
            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(0));
            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages, Has.Member("Store purchase failed."));
            Assert.That(view.LastStatus, Is.EqualTo("Store purchase failed."));
        }

        [Test]
        public void Purchase_WhenVerificationFails_DoesNotConsume_AndShowsError()
        {
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Succeeded,
                    "google_token_123",
                    "txn_1",
                    "premium_sku",
                    string.Empty))
            };
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
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", _ => { }),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(0));
            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages, Has.Member("Stub verifier failed the purchase token."));
            Assert.That(view.LastStatus, Is.EqualTo("Stub verifier failed the purchase token."));
        }

        [Test]
        public void Purchase_WhenConsumeFails_ShowsRecoverableError_AndKeepsPopupOpen()
        {
            var purchaseService = new StubBattlePassPurchaseService
            {
                PurchaseResponseFactory = _ => UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Succeeded,
                    "google_token_123",
                    "txn_1",
                    "premium_sku",
                    string.Empty)),
                ConsumeResponseFactory = (_, _) => UniTask.FromResult(new BattlePassConsumeResult(
                    BattlePassConsumeStatus.Failed,
                    "Store confirmation failed."))
            };
            var serverService = new StubBattlePassServerService
            {
                VerifyPurchaseResponseFactory = (_, _, _) => UniTask.FromResult(new BattlePassPurchaseVerificationResult(
                    true,
                    "acknowledged",
                    CreatePremiumUserState(),
                    "battle_pass",
                    "premium_sku",
                    "active",
                    null,
                    null))
            };
            BattlePassPurchaseVerificationResult callbackResult = null;
            var controller = CreateController(
                purchaseService,
                serverService,
                new BattlePassIAPWindowArgs("season_1", "premium_sku", result => callbackResult = result),
                out var view);

            RunCoroutine(controller.Show(controller.TestArgs));
            view.EmitPurchaseClick();

            Assert.That(purchaseService.ConsumeCalls, Is.EqualTo(1));
            Assert.That(callbackResult, Is.Null);
            Assert.That(controller.CloseCalls, Is.EqualTo(0));
            Assert.That(controller.InfoMessages.Count, Is.EqualTo(1));
            Assert.That(controller.InfoMessages[0], Does.Contain("granted"));
            Assert.That(controller.InfoMessages[0], Does.Contain("Store confirmation failed."));
            Assert.That(view.LastStatus, Does.Contain("Store confirmation failed."));
        }

        private TestBattlePassIAPWindowController CreateController(
            StubBattlePassPurchaseService purchaseService,
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
            constructMethod.Invoke(controller, new object[] { purchaseService, serverService });

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

            protected override void ShowInfo(string message)
            {
                InfoMessages.Add(message);
            }

            protected override void CloseWindow()
            {
                CloseCalls++;
            }
        }

        private sealed class StubBattlePassPurchaseService : IBattlePassPurchaseService
        {
            public int PurchaseCalls { get; private set; }
            public int ConsumeCalls { get; private set; }
            public string LastPurchasedProductId { get; private set; }
            public string LastConsumedProductId { get; private set; }
            public string LastConsumedToken { get; private set; }
            public Func<string, UniTask<BattlePassStorePurchaseResult>> PurchaseResponseFactory { get; set; }
            public Func<string, string, UniTask<BattlePassConsumeResult>> ConsumeResponseFactory { get; set; }

            public UniTask<BattlePassStorePurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                PurchaseCalls++;
                LastPurchasedProductId = productId;

                if (PurchaseResponseFactory != null)
                {
                    return PurchaseResponseFactory(productId);
                }

                return UniTask.FromResult(new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Succeeded,
                    "google_token_default",
                    "txn_default",
                    productId,
                    string.Empty));
            }

            public UniTask<BattlePassConsumeResult> ConsumeAsync(string productId, string purchaseToken, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                ConsumeCalls++;
                LastConsumedProductId = productId;
                LastConsumedToken = purchaseToken;

                if (ConsumeResponseFactory != null)
                {
                    return ConsumeResponseFactory(productId, purchaseToken);
                }

                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Succeeded, string.Empty));
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
