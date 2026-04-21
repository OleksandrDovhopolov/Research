using System;
using UnityEngine;

namespace Rewards
{
    public static class RewardedAdsProviderFactory
    {
        public static IRewardedAdsProvider Create(RewardedAdsConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IRewardedAdsProvider provider;
            switch (config.Mode)
            {
                case RewardedAdsMode.Mock:
                    provider = new MockRewardedAdsProvider(config);
                    break;
                case RewardedAdsMode.UnityAdsTestMode:
                    provider = new UnityAdsRewardedAdsProvider(config);
                    break;
                case RewardedAdsMode.LevelPlay:
                    provider = new LevelPlayRewardedAdsProvider(config);
                    break;
                default:
                    provider = new MockRewardedAdsProvider(config);
                    break;
            }

            Debug.Log($"[RewardAds] Provider selected. Mode={config.Mode}, Provider={provider.GetType().Name}");
            return provider;
        }
    }
}
