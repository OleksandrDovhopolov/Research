using System;
using System.Threading;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    public sealed class LevelPlayRewardedAdsProviderTests
    {
        [Test]
        public void Factory_Create_ReturnsLevelPlayProvider_WhenModeLevelPlay()
        {
            var config = new RewardedAdsConfig
            {
                Mode = RewardedAdsMode.LevelPlay
            };

            var provider = RewardedAdsProviderFactory.Create(config);

            Assert.That(provider, Is.TypeOf<LevelPlayRewardedAdsProvider>());
        }

#if !UNITY_LEVELPLAY
        [Test]
        public void Provider_Fallback_Throws_WhenSdkIsMissing()
        {
            var provider = new LevelPlayRewardedAdsProvider(new RewardedAdsConfig());

            Assert.That(provider.IsInitialized, Is.False);
            Assert.That(provider.IsAdReady("rewarded"), Is.False);

            Assert.Throws<InvalidOperationException>(() =>
                provider.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() =>
                provider.PreloadAsync("rewarded", CancellationToken.None).GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() =>
                provider.ShowAsync("rewarded", "ri_test", CancellationToken.None).GetAwaiter().GetResult());
        }
#endif
    }
}
