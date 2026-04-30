using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassPurchaseService
    {
        UniTask<BattlePassStorePurchaseResult> PurchaseAsync(string productId, CancellationToken ct = default);
        UniTask<BattlePassConsumeResult> ConsumeAsync(string productId, string purchaseToken, CancellationToken ct = default);
    }
}
