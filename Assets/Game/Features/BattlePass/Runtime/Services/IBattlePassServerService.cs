using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassServerService
    {
        UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default);
        UniTask<BattlePassAddXpResult> AddXpAsync(int amount, CancellationToken ct = default);
        UniTask<BattlePassClaimResult> ClaimAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct = default);
        [Obsolete("Use IBattlePassPurchaseVerify instead")] UniTask<BattlePassPurchaseVerificationResult> VerifyGooglePurchaseAsync(string seasonId, string productId, string purchaseToken, CancellationToken ct = default);
    }
}
