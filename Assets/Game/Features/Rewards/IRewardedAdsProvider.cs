using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardedAdsProvider
    {
        bool IsInitialized { get; }
        bool IsAdReady(string adUnitId);

        UniTask InitializeAsync(CancellationToken ct = default);
        UniTask PreloadAsync(string adUnitId, CancellationToken ct = default);
        UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default);
    }
}
