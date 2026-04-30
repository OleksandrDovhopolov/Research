using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace BattlePass
{
    public class BattlePassIAPWindowArgs : WindowArgs
    {
        public BattlePassIAPWindowArgs(
            string seasonId,
            string productId,
            Action<BattlePassPurchaseVerificationResult> onPurchaseVerified)
        {
            SeasonId = seasonId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            OnPurchaseVerified = onPurchaseVerified;
        }

        public string SeasonId { get; }
        public string ProductId { get; }
        public Action<BattlePassPurchaseVerificationResult> OnPurchaseVerified { get; }
    }

    [Window("BattlePassPremiumWindow", WindowType.Popup)]
    public class BattlePassIAPWindowController : WindowController<BattlePassIAPWindowView>
    {
        private const string DefaultTitle = "Unlock Premium Battle Pass";
        private const string DefaultButtonLabel = "Buy Premium";
        private const string DefaultPricePlaceholder = "Loading...";
        private const string PendingMessage = "Purchase is processing.";
        private const string CancelledMessage = "Purchase was cancelled.";
        private const string CompletedMessage = "Purchase completed successfully.";
        private const string CompletedInfoMessage = "Battle Pass premium purchase completed successfully.";
        private const string GrantedConsumeFailureMessage =
            "Battle Pass premium was granted, but the store purchase could not be finalized. Repurchase may be unavailable until this is retried.";

        private IBattlePassPurchaseService _battlePassPurchaseService;
        private IBattlePassServerService _battlePassServerService;
        private CancellationTokenSource _purchaseCts;
        private bool _isVerificationInFlight;

        private BattlePassIAPWindowArgs Args => (BattlePassIAPWindowArgs)Arguments;

        [Inject]
        private void Construct(
            IBattlePassPurchaseService battlePassPurchaseService,
            IBattlePassServerService battlePassServerService)
        {
            _battlePassPurchaseService = battlePassPurchaseService;
            _battlePassServerService = battlePassServerService;
        }

        protected override void OnShowStart()
        {
            ResetPurchaseCts();
            _isVerificationInFlight = false;
            View.ResetView();
            View.SetTitle(DefaultTitle);
            View.SetSeason(Args?.SeasonId);
            View.SetPrice(DefaultPricePlaceholder);
            View.SetPurchaseButtonLabel(DefaultButtonLabel);
            RefreshPriceAsync(_purchaseCts.Token).Forget();
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.PurchaseClick += HandlePurchaseClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.PurchaseClick -= HandlePurchaseClicked;
            View.CloseClick -= CloseWindow;

            CancelPurchase();
            _isVerificationInFlight = false;
            View.SetPurchaseButtonInteractable(true);
        }

        private void HandlePurchaseClicked()
        {
            if (_isVerificationInFlight)
            {
                return;
            }

            PurchaseAndVerifyAsync(_purchaseCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid RefreshPriceAsync(CancellationToken ct)
        {
            if (Args == null)
            {
                return;
            }

            try
            {
                var price = await _battlePassPurchaseService.GetDisplayPriceAsync(Args.ProductId, ct);
                ct.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(price))
                {
                    View.SetPrice(price);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BattlePassIAPWindowController] Failed to resolve display price. {exception.Message}");
            }
        }

        private async UniTaskVoid PurchaseAndVerifyAsync(CancellationToken ct)
        {
            if (Args == null)
            {
                ShowInfo("Battle Pass purchase window arguments are missing.");
                return;
            }

            _isVerificationInFlight = true;
            View.SetPurchaseButtonInteractable(false);
            View.SetStatus("Starting purchase...");

            try
            {
                var purchaseResult = await _battlePassPurchaseService.PurchaseAsync(Args.ProductId, ct);
                ct.ThrowIfCancellationRequested();

                switch (purchaseResult.Status)
                {
                    case BattlePassStorePurchaseStatus.Pending:
                        View.SetStatus(PendingMessage);
                        ShowInfo(PendingMessage);
                        return;
                    case BattlePassStorePurchaseStatus.Cancelled:
                        View.SetStatus(CancelledMessage);
                        return;
                    case BattlePassStorePurchaseStatus.Failed:
                        var purchaseFailureMessage = string.IsNullOrWhiteSpace(purchaseResult.ErrorMessage)
                            ? "Purchase failed before verification."
                            : purchaseResult.ErrorMessage;
                        View.SetStatus(purchaseFailureMessage);
                        ShowInfo(purchaseFailureMessage);
                        return;
                }

                if (string.IsNullOrWhiteSpace(purchaseResult.PurchaseToken))
                {
                    const string missingTokenMessage = "Google Play purchase token was not returned.";
                    View.SetStatus(missingTokenMessage);
                    ShowInfo(missingTokenMessage);
                    return;
                }

                View.SetStatus("Verifying purchase...");
                var result = await _battlePassServerService.VerifyGooglePurchaseAsync(
                    Args.SeasonId,
                    Args.ProductId,
                    purchaseResult.PurchaseToken,
                    ct);
                ct.ThrowIfCancellationRequested();

                var shouldConsume = ShouldConsumeAfterVerify(result);
                if (shouldConsume)
                {
                    var consumeResult = await _battlePassPurchaseService.ConsumeAsync(
                        Args.ProductId,
                        purchaseResult.PurchaseToken,
                        ct);
                    ct.ThrowIfCancellationRequested();

                    if (!consumeResult.Success)
                    {
                        var consumeFailureMessage = string.IsNullOrWhiteSpace(consumeResult.ErrorMessage)
                            ? GrantedConsumeFailureMessage
                            : $"{GrantedConsumeFailureMessage} {consumeResult.ErrorMessage}";
                        Debug.LogError($"[BattlePassIAPWindowController] {consumeFailureMessage}");
                        View.SetStatus(consumeFailureMessage);
                        ShowInfo(consumeFailureMessage);
                        return;
                    }
                }

                if (result.Success)
                {
                    Args.OnPurchaseVerified?.Invoke(result);
                    View.SetStatus(CompletedMessage);
                    ShowInfo(CompletedInfoMessage);
                    CloseWindow();
                    return;
                }

                if (IsGranted(result))
                {
                    Args.OnPurchaseVerified?.Invoke(result);
                    View.SetStatus(CompletedMessage);
                    ShowInfo(CompletedInfoMessage);
                    CloseWindow();
                    return;
                }

                if (IsPending(result))
                {
                    View.SetStatus(PendingMessage);
                    ShowInfo(PendingMessage);
                    return;
                }

                var failureMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? $"Purchase verification failed: {result.ErrorCode}"
                    : result.ErrorMessage;
                View.SetStatus(failureMessage);
                ShowInfo(failureMessage);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                var message = $"Battle Pass purchase flow failed. {exception.Message}";
                Debug.LogError($"[BattlePassIAPWindowController] {message}");
                View.SetStatus(message);
                ShowInfo(message);
            }
            finally
            {
                _isVerificationInFlight = false;
                View.SetPurchaseButtonInteractable(true);
            }
        }

        private static bool IsPending(BattlePassPurchaseVerificationResult result)
        {
            return result != null &&
                   string.Equals(result.PurchaseStatus, "pending", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGranted(BattlePassPurchaseVerificationResult result)
        {
            return result != null &&
                   string.Equals(result.PurchaseStatus, "granted", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldConsumeAfterVerify(BattlePassPurchaseVerificationResult result)
        {
            return result != null && (result.Success || IsGranted(result));
        }

        protected virtual void ShowInfo(string message)
        {
            UIManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        protected virtual void CloseWindow()
        {
            UIManager.Hide<BattlePassIAPWindowController>();
        }

        private void ResetPurchaseCts()
        {
            CancelPurchase();
            _purchaseCts = new CancellationTokenSource();
        }

        private void CancelPurchase()
        {
            if (_purchaseCts == null)
            {
                return;
            }

            _purchaseCts.Cancel();
            _purchaseCts.Dispose();
            _purchaseCts = null;
        }
    }
}
