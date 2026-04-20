using UnityEngine;

namespace Rewards
{
    [CreateAssetMenu(
        fileName = "RewardedAdsConfig",
        menuName = "Game/Rewards/Rewarded Ads Config",
        order = 1)]
    public sealed class RewardedAdsConfigSO : ScriptableObject
    {
        public RewardedAdsConfig Config = new();

        public RewardedAdsConfig GetOrCreate()
        {
            Config ??= new RewardedAdsConfig();
            return Config;
        }
    }
}
