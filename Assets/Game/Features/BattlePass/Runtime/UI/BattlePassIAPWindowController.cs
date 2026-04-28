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
        private const string DefaultButtonLabel = "Verify Purchase";

        private IBattlePassServerService _battlePassServerService;
        private CancellationTokenSource _purchaseCts;
        private bool _isVerificationInFlight;

        private BattlePassIAPWindowArgs Args => (BattlePassIAPWindowArgs)Arguments;

        [Inject]
        private void Construct(IBattlePassServerService battlePassServerService)
        {
            _battlePassServerService = battlePassServerService;
        }

        protected override void OnShowStart()
        {
            ResetPurchaseCts();
            _isVerificationInFlight = false;
            View.ResetView();
            View.SetTitle(DefaultTitle);
            View.SetSeason(Args?.SeasonId);
            View.SetProduct(Args?.ProductId);
            View.SetPurchaseButtonLabel(DefaultButtonLabel);
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

            VerifyPurchaseAsync(_purchaseCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid VerifyPurchaseAsync(CancellationToken ct)
        {
            if (Args == null)
            {
                ShowInfo("Battle Pass purchase window arguments are missing.");
                return;
            }

            _isVerificationInFlight = true;
            View.SetPurchaseButtonInteractable(false);
            View.SetStatus("Verifying purchase...");

            try
            {
                var token = GeneratePurchaseToken();
                var result = await _battlePassServerService.VerifyGooglePurchaseAsync(Args.SeasonId, Args.ProductId, token, ct);
                ct.ThrowIfCancellationRequested();

                if (result.Success)
                {
                    Args.OnPurchaseVerified?.Invoke(result);
                    View.SetStatus("Purchase completed successfully.");
                    ShowInfo("Battle Pass premium purchase completed successfully.");
                    CloseWindow();
                    return;
                }

                if (string.Equals(result.PurchaseStatus, "granted", StringComparison.OrdinalIgnoreCase))
                {
                    Args.OnPurchaseVerified?.Invoke(result);
                    var grantedMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Battle Pass premium was granted, but acknowledge failed."
                        : result.ErrorMessage;
                    View.SetStatus(grantedMessage);
                    ShowInfo(grantedMessage);
                    CloseWindow();
                    return;
                }

                if (string.Equals(result.PurchaseStatus, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    const string pendingMessage = "Purchase is processing.";
                    View.SetStatus(pendingMessage);
                    ShowInfo(pendingMessage);
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
                var message = $"Battle Pass purchase verification request failed. {exception.Message}";
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

        protected virtual string GeneratePurchaseToken()
        {
            return $"mock_premium_{Guid.NewGuid():N}";
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
