using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace BattlePass
{
    public sealed class UnityIapBattlePassPurchaseService : IBattlePassPurchaseService, IDetailedStoreListener
    {
        private const string Tag = "[BattlePassIAP]";
        private const string GooglePlayStoreName = "GooglePlay";

        private readonly HashSet<string> _knownProductIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Product> _pendingProductsByToken = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Product> _pendingProductsById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BattlePassStorePurchaseResult> _pendingResultsByProductId = new(StringComparer.Ordinal);

        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private UniTaskCompletionSource<bool> _initializationTcs;
        private UniTaskCompletionSource<bool> _fetchProductsTcs;
        private UniTaskCompletionSource<BattlePassStorePurchaseResult> _purchaseTcs;
        private string _activePurchaseProductId;
        private bool _isInitializing;

        public async UniTask<string> GetDisplayPriceAsync(string productId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return string.Empty;
            }

            ct.ThrowIfCancellationRequested();
            await EnsureStoreReadyAsync(productId, ct);
            ct.ThrowIfCancellationRequested();

            var product = _storeController?.products?.WithID(productId);
            return product?.metadata?.localizedPriceString ?? string.Empty;
        }

        public async UniTask<BattlePassStorePurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new InvalidOperationException("Product id is empty.");
            }

            ct.ThrowIfCancellationRequested();
            await EnsureStoreReadyAsync(productId, ct);
            ct.ThrowIfCancellationRequested();

            if (_pendingResultsByProductId.TryGetValue(productId, out var pendingResult))
            {
                return pendingResult;
            }

            var product = _storeController?.products?.WithID(productId);
            if (product == null)
            {
                return new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Failed,
                    string.Empty,
                    string.Empty,
                    productId,
                    $"Product '{productId}' was not loaded from Unity IAP.");
            }

            if (_purchaseTcs != null)
            {
                return new BattlePassStorePurchaseResult(
                    BattlePassStorePurchaseStatus.Failed,
                    string.Empty,
                    string.Empty,
                    productId,
                    "Another purchase is already in progress.");
            }

            _activePurchaseProductId = productId;
            _purchaseTcs = new UniTaskCompletionSource<BattlePassStorePurchaseResult>();

            try
            {
                _storeController.InitiatePurchase(product);
                return await _purchaseTcs.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                _activePurchaseProductId = null;
                _purchaseTcs = null;
            }
        }

        public UniTask<BattlePassConsumeResult> ConsumeAsync(string productId, string purchaseToken, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning(
                $"{Tag} ConsumeAsync called. ProductId='{productId}', " +
                $"TokenPrefix='{GetTokenPrefix(purchaseToken)}'.");

            if (string.IsNullOrWhiteSpace(productId))
            {
                Debug.LogWarning($"{Tag} ConsumeAsync aborted because product id is empty.");
                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Failed, "Product id is empty."));
            }

            if (string.IsNullOrWhiteSpace(purchaseToken))
            {
                Debug.LogWarning($"{Tag} ConsumeAsync aborted because purchase token is empty.");
                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Failed, "Purchase token is empty."));
            }

            if (_storeController == null)
            {
                Debug.LogWarning($"{Tag} ConsumeAsync aborted because Unity IAP is not initialized.");
                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Failed, "Unity IAP is not initialized."));
            }

            if (!_pendingProductsByToken.TryGetValue(purchaseToken, out var product))
            {
                Debug.LogWarning(
                    $"{Tag} Pending product was not found by token. " +
                    $"Trying product id lookup for '{productId}'.");
                _pendingProductsById.TryGetValue(productId, out product);
            }

            if (product == null)
            {
                Debug.LogWarning(
                    $"{Tag} ConsumeAsync could not find pending purchase. " +
                    $"ProductId='{productId}', TokenPrefix='{GetTokenPrefix(purchaseToken)}'.");
                return UniTask.FromResult(new BattlePassConsumeResult(
                    BattlePassConsumeStatus.Failed,
                    $"Pending purchase for product '{productId}' was not found."));
            }

            try
            {
                Debug.LogWarning(
                    $"{Tag} ConfirmPendingPurchase starting. " +
                    $"ProductId='{product.definition.id}', TransactionId='{product.transactionID}'.");
                _storeController.ConfirmPendingPurchase(product);
                Debug.LogWarning(
                    $"{Tag} ConfirmPendingPurchase completed. " +
                    $"ProductId='{product.definition.id}', TokenPrefix='{GetTokenPrefix(purchaseToken)}'.");
                RemovePendingPurchase(product.definition.id, purchaseToken);
                Debug.LogWarning(
                    $"{Tag} Pending purchase removed from local cache. " +
                    $"ProductId='{product.definition.id}'.");
                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Succeeded, string.Empty));
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Tag} Failed to confirm pending purchase for '{productId}'. {exception}");
                return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Failed, exception.Message));
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            _isInitializing = false;
            _initializationTcs?.TrySetResult(true);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _isInitializing = false;
            _initializationTcs?.TrySetException(new InvalidOperationException(
                $"{Tag} Unity IAP initialization failed. Reason={error}, Message={message}"));
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            var product = e?.purchasedProduct;
            var productId = product?.definition?.id ?? string.Empty;
            var purchaseToken = TryExtractGooglePlayPurchaseToken(product?.receipt);
            Debug.LogWarning(
                $"{Tag} ProcessPurchase received. ProductId='{productId}', " +
                $"TransactionId='{product?.transactionID}', TokenPrefix='{GetTokenPrefix(purchaseToken)}'.");

            if (string.IsNullOrWhiteSpace(productId))
            {
                CompletePurchaseFailure("Unity IAP returned a purchase without product id.");
                return PurchaseProcessingResult.Pending;
            }

            if (string.IsNullOrWhiteSpace(purchaseToken))
            {
                CompletePurchaseFailure($"Failed to extract Google Play purchaseToken for '{productId}'.");
                return PurchaseProcessingResult.Pending;
            }

            var result = new BattlePassStorePurchaseResult(
                BattlePassStorePurchaseStatus.Succeeded,
                purchaseToken,
                product.transactionID,
                productId,
                string.Empty);

            _pendingProductsByToken[purchaseToken] = product;
            _pendingProductsById[productId] = product;
            _pendingResultsByProductId[productId] = result;

            if (_purchaseTcs != null && string.Equals(_activePurchaseProductId, productId, StringComparison.Ordinal))
            {
                _purchaseTcs.TrySetResult(result);
            }

            Debug.LogWarning(
                $"{Tag} ProcessPurchase stored pending purchase and returned Pending. " +
                $"ProductId='{productId}'.");
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var message = $"{Tag} Purchase failed for '{product?.definition?.id ?? "<unknown>"}'. Reason={failureReason}";
            CompletePurchaseResult(product?.definition?.id, failureReason == PurchaseFailureReason.UserCancelled
                ? new BattlePassStorePurchaseResult(BattlePassStorePurchaseStatus.Cancelled, string.Empty, string.Empty, product?.definition?.id, "Purchase was cancelled.")
                : new BattlePassStorePurchaseResult(BattlePassStorePurchaseStatus.Failed, string.Empty, string.Empty, product?.definition?.id, message));
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var productId = failureDescription?.productId ?? product?.definition?.id;
            var message = string.IsNullOrWhiteSpace(failureDescription?.message)
                ? $"{Tag} Purchase failed for '{productId ?? "<unknown>"}'. Reason={failureDescription?.reason}"
                : failureDescription.message;

            var status = failureDescription?.reason == PurchaseFailureReason.UserCancelled
                ? BattlePassStorePurchaseStatus.Cancelled
                : BattlePassStorePurchaseStatus.Failed;

            CompletePurchaseResult(productId, new BattlePassStorePurchaseResult(
                status,
                string.Empty,
                string.Empty,
                productId,
                message));
        }

        private async UniTask EnsureStoreReadyAsync(string productId, CancellationToken ct)
        {
            if (_storeController == null)
            {
                await InitializeAsync(productId, ct);
                return;
            }

            if (_storeController.products?.WithID(productId) != null)
            {
                return;
            }

            await FetchAdditionalProductAsync(productId, ct);
        }

        private async UniTask InitializeAsync(string productId, CancellationToken ct)
        {
            if (_storeController != null)
            {
                return;
            }

            if (_isInitializing)
            {
                await _initializationTcs.Task.AttachExternalCancellation(ct);
                return;
            }

            _isInitializing = true;
            _knownProductIds.Add(productId);
            _initializationTcs = new UniTaskCompletionSource<bool>();

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var knownProductId in _knownProductIds)
            {
                builder.AddProduct(knownProductId, ProductType.Consumable);
            }

            UnityPurchasing.Initialize(this, builder);
            await _initializationTcs.Task.AttachExternalCancellation(ct);
        }

        private async UniTask FetchAdditionalProductAsync(string productId, CancellationToken ct)
        {
            if (_storeController == null)
            {
                throw new InvalidOperationException("Unity IAP controller is not initialized.");
            }

            _knownProductIds.Add(productId);
            _fetchProductsTcs = new UniTaskCompletionSource<bool>();
            _storeController.FetchAdditionalProducts(
                new HashSet<ProductDefinition> { new ProductDefinition(productId, ProductType.Consumable) },
                () => _fetchProductsTcs.TrySetResult(true),
                (reason, message) => _fetchProductsTcs.TrySetException(new InvalidOperationException(
                    $"{Tag} Failed to fetch Unity IAP product '{productId}'. Reason={reason}, Message={message}")));
            await _fetchProductsTcs.Task.AttachExternalCancellation(ct);
            _fetchProductsTcs = null;
        }

        private void CompletePurchaseFailure(string message)
        {
            Debug.LogError($"{Tag} {message}");

            if (_purchaseTcs == null)
            {
                return;
            }

            _purchaseTcs.TrySetResult(new BattlePassStorePurchaseResult(
                BattlePassStorePurchaseStatus.Failed,
                string.Empty,
                string.Empty,
                _activePurchaseProductId,
                message));
        }

        private void CompletePurchaseResult(string productId, BattlePassStorePurchaseResult result)
        {
            if (_purchaseTcs == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_activePurchaseProductId) &&
                !string.Equals(_activePurchaseProductId, productId, StringComparison.Ordinal))
            {
                return;
            }

            _purchaseTcs.TrySetResult(result);
        }

        private void RemovePendingPurchase(string productId, string purchaseToken)
        {
            if (!string.IsNullOrWhiteSpace(purchaseToken))
            {
                _pendingProductsByToken.Remove(purchaseToken);
            }

            if (!string.IsNullOrWhiteSpace(productId))
            {
                _pendingProductsById.Remove(productId);
                _pendingResultsByProductId.Remove(productId);
            }
        }

        private static string TryExtractGooglePlayPurchaseToken(string receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt))
            {
                return string.Empty;
            }

            try
            {
                var root = JObject.Parse(receipt);
                var storeName = root["Store"]?.Value<string>();
                if (!string.Equals(storeName, GooglePlayStoreName, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                var payloadValue = root["Payload"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(payloadValue))
                {
                    return string.Empty;
                }

                var payload = JObject.Parse(payloadValue);
                var directToken = payload["purchaseToken"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(directToken))
                {
                    return directToken;
                }

                var purchaseJson = payload["json"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(purchaseJson))
                {
                    return string.Empty;
                }

                return JObject.Parse(purchaseJson)["purchaseToken"]?.Value<string>() ?? string.Empty;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Tag} Failed to parse Google Play purchase token from receipt. {exception}");
                return string.Empty;
            }
        }

        private static string GetTokenPrefix(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "<empty>";
            }

            return token.Length <= 8 ? token : token.Substring(0, 8);
        }
    }
}
