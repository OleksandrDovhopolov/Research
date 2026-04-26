using System.Threading;
using Cysharp.Threading.Tasks;

namespace BattlePass
{
    public interface IBattlePassServerService
    {
        UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default);
        UniTask<BattlePassUserState> AddXpAsync(int amount, CancellationToken ct = default);
        UniTask<BattlePassClaimResult> ClaimAsync(string seasonId, int level, BattlePassRewardTrack rewardTrack, CancellationToken ct = default);
    }
}
