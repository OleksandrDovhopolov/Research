using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public sealed class MockBattlePassPurchaseService : IBattlePassPurchaseService
    {
        public UniTask<BattlePassStorePurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return UniTask.FromResult(new BattlePassStorePurchaseResult(
                BattlePassStorePurchaseStatus.Succeeded,
                $"mock_premium_{Guid.NewGuid():N}",
                Guid.NewGuid().ToString("N"),
                productId,
                string.Empty));
        }

        public UniTask<BattlePassConsumeResult> ConsumeAsync(string productId, string purchaseToken, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult(new BattlePassConsumeResult(BattlePassConsumeStatus.Succeeded, string.Empty));
        }
    }
}
