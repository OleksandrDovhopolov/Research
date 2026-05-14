using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassPurchaseVerify
    {
        UniTask<BattlePassPurchaseVerificationResult> VerifyGooglePurchaseAsync(string seasonId, string productId, string purchaseToken, CancellationToken ct = default);
    }
}
