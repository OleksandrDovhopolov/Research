namespace BattlePass
{
    public enum BattlePassStorePurchaseStatus
    {
        Succeeded = 0,
        Pending = 1,
        Cancelled = 2,
        Failed = 3
    }

    public enum BattlePassConsumeStatus
    {
        Succeeded = 0,
        Failed = 1
    }

    public readonly struct BattlePassStorePurchaseResult
    {
        public BattlePassStorePurchaseResult(
            BattlePassStorePurchaseStatus status,
            string purchaseToken,
            string storeTransactionId,
            string productId,
            string errorMessage)
        {
            Status = status;
            PurchaseToken = purchaseToken ?? string.Empty;
            StoreTransactionId = storeTransactionId ?? string.Empty;
            ProductId = productId ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public BattlePassStorePurchaseStatus Status { get; }
        public string PurchaseToken { get; }
        public string StoreTransactionId { get; }
        public string ProductId { get; }
        public string ErrorMessage { get; }
    }

    public readonly struct BattlePassConsumeResult
    {
        public BattlePassConsumeResult(BattlePassConsumeStatus status, string errorMessage)
        {
            Status = status;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public BattlePassConsumeStatus Status { get; }
        public string ErrorMessage { get; }
        public bool Success => Status == BattlePassConsumeStatus.Succeeded;
    }
}
