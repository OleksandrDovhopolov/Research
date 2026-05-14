using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private const string PendingFinalizationMessage =
            "Purchase granted, but final processing is still pending. Please wait before trying to repurchase this item.";
        private const string GrantedConsumeFailureMessage =
            "Battle Pass premium was granted, but the store purchase could not be finalized. Repurchase may be unavailable until this is retried.";
        private const string PurchaseStatusVerifiedAndConsumed = "verified_and_consumed";
        private const string PurchaseStatusVerifiedAndAcknowledged = "verified_and_acknowledged";
        private const string PurchaseStatusDuplicateAlreadyProcessed = "duplicate_already_processed";
        private const string PurchaseStatusGrantedButFinalizationPending = "granted_but_finalization_pending";
        private const string PurchaseStatusVerifyFailed = "verify_failed";

        private IBattlePassPurchaseService _battlePassPurchaseService;
        private IBattlePassPurchaseVerify _battlePassPurchaseVerify;
        private IBattlePassWindowRouter _windowRouter;
        private CancellationTokenSource _purchaseCts;
        private bool _isVerificationInFlight;

        private BattlePassIAPWindowArgs Args => (BattlePassIAPWindowArgs)Arguments;

        [Inject]
        private void Construct(
            IBattlePassPurchaseService battlePassPurchaseService,
            IBattlePassPurchaseVerify battlePassPurchaseVerify,
            IBattlePassWindowRouter windowRouter)
        {
            _battlePassPurchaseService = battlePassPurchaseService;
            _battlePassPurchaseVerify = battlePassPurchaseVerify;
            _windowRouter = windowRouter;
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

            Debug.LogWarning(
                $"[BattlePassIAPWindowController] Purchase flow started. " +
                $"SeasonId='{Args.SeasonId}', ProductId='{Args.ProductId}'.");
            _isVerificationInFlight = true;
            View.SetPurchaseButtonInteractable(false);
            View.SetStatus("Starting purchase...");

            try
            {
                var purchaseResult = await _battlePassPurchaseService.PurchaseAsync(Args.ProductId, ct);
                ct.ThrowIfCancellationRequested();
                Debug.LogWarning(
                    $"[BattlePassIAPWindowController] PurchaseAsync completed. " +
                    $"Status={purchaseResult.Status}, ProductId='{purchaseResult.ProductId}', " +
                    $"HasToken={!string.IsNullOrWhiteSpace(purchaseResult.PurchaseToken)}, " +
                    $"TransactionId='{purchaseResult.StoreTransactionId}'.");

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
                Debug.LogWarning(
                    $"[BattlePassIAPWindowController] Starting backend verify. " +
                    $"SeasonId='{Args.SeasonId}', ProductId='{Args.ProductId}', " +
                    $"TokenPrefix='{GetTokenPrefix(purchaseResult.PurchaseToken)}'.");
                var result = await _battlePassPurchaseVerify.VerifyGooglePurchaseAsync(
                    Args.SeasonId,
                    Args.ProductId,
                    purchaseResult.PurchaseToken,
                    ct);
                ct.ThrowIfCancellationRequested();
                var purchaseStatus = NormalizeStatus(result?.PurchaseStatus);
                Debug.LogWarning(
                    $"[BattlePassIAPWindowController] Verify completed. " +
                    $"ProductId='{Args.ProductId}', TokenPrefix='{GetTokenPrefix(purchaseResult.PurchaseToken)}', " +
                    $"Success={result?.Success}, PurchaseStatus='{purchaseStatus}', " +
                    $"GoogleFinalizeStatus='{result?.GoogleFinalizeStatus}', " +
                    $"ErrorCode='{result?.ErrorCode}', ErrorMessage='{result?.ErrorMessage}'.");

                var shouldConsume = IsFinalSuccessPurchaseStatus(purchaseStatus);
                Debug.LogWarning(
                    $"[BattlePassIAPWindowController] Consume decision evaluated. " +
                    $"ShouldConsume={shouldConsume}, ProductId='{Args.ProductId}', PurchaseStatus='{purchaseStatus}'.");
                if (shouldConsume)
                {
                    Debug.LogWarning(
                        $"[BattlePassIAPWindowController] Calling ConsumeAsync. " +
                        $"ProductId='{Args.ProductId}', TokenPrefix='{GetTokenPrefix(purchaseResult.PurchaseToken)}'.");
                    var consumeResult = await _battlePassPurchaseService.ConsumeAsync(
                        Args.ProductId,
                        purchaseResult.PurchaseToken,
                        ct);
                    ct.ThrowIfCancellationRequested();
                    Debug.LogWarning(
                        $"[BattlePassIAPWindowController] ConsumeAsync completed. " +
                        $"Success={consumeResult.Success}, Status={consumeResult.Status}, " +
                        $"ErrorMessage='{consumeResult.ErrorMessage}'.");

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
                    Args.OnPurchaseVerified?.Invoke(result);
                    View.SetStatus(CompletedMessage);
                    ShowInfo(CompletedInfoMessage);
                    CloseWindow();
                    return;
                }

                if (string.Equals(
                        purchaseStatus,
                        PurchaseStatusGrantedButFinalizationPending,
                        StringComparison.OrdinalIgnoreCase))
                {
                    View.SetStatus(PendingFinalizationMessage);
                    ShowInfo(PendingFinalizationMessage);
                    return;
                }

                var failureMessage = BuildVerifyFailureMessage(result, purchaseStatus);
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
                Debug.LogWarning(
                    $"[BattlePassIAPWindowController] Purchase flow finished. " +
                    $"ProductId='{Args.ProductId}', VerificationInFlight->false.");
                _isVerificationInFlight = false;
                View.SetPurchaseButtonInteractable(true);
            }
        }

        private static bool IsFinalSuccessPurchaseStatus(string purchaseStatus)
        {
            return string.Equals(purchaseStatus, PurchaseStatusVerifiedAndConsumed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(purchaseStatus, PurchaseStatusVerifiedAndAcknowledged, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(purchaseStatus, PurchaseStatusDuplicateAlreadyProcessed, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeStatus(string purchaseStatus)
        {
            return purchaseStatus?.Trim() ?? string.Empty;
        }

        private static string BuildVerifyFailureMessage(BattlePassPurchaseVerificationResult result, string purchaseStatus)
        {
            if (!string.IsNullOrWhiteSpace(result?.ErrorMessage))
            {
                return result.ErrorMessage;
            }

            if (string.Equals(purchaseStatus, PurchaseStatusVerifyFailed, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(result?.ErrorCode))
            {
                return $"Purchase verification failed: {result.ErrorCode}";
            }

            if (!string.IsNullOrWhiteSpace(result?.ErrorCode))
            {
                return $"Purchase verification failed: {result.ErrorCode}";
            }

            if (!string.IsNullOrWhiteSpace(purchaseStatus))
            {
                return $"Purchase verification failed: {purchaseStatus}";
            }

            return "Purchase verification failed.";
        }

        private static string GetTokenPrefix(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "<empty>";
            }

            return token.Length <= 8 ? token : token.Substring(0, 8);
        }

        protected virtual void ShowInfo(string message)
        {
            _windowRouter?.ShowInfo(message);
        }

        protected virtual void CloseWindow()
        {
            _windowRouter?.HideBattlePassPurchaseWindow();
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
