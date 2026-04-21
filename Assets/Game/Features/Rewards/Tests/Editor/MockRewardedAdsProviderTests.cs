using System.Threading;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    public sealed class MockRewardedAdsProviderTests
    {
        [Test]
        public void ShowAsync_ReturnsCompleted_WhenOutcomeSuccess()
        {
            var config = new RewardedAdsConfig
            {
                MockOutcome = MockAdsOutcome.Success,
                UseRandomMockDelay = false,
                MockDelaySeconds = 0.01f
            };
            var provider = new MockRewardedAdsProvider(config);

            provider.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            provider.PreloadAsync("rewarded", CancellationToken.None).GetAwaiter().GetResult();
            var result = provider.ShowAsync("rewarded", "ri_test", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(RewardedShowResult.Completed));
        }

        [Test]
        public void ShowAsync_ReturnsCanceled_WhenOutcomeCancel()
        {
            var config = new RewardedAdsConfig
            {
                MockOutcome = MockAdsOutcome.Cancel,
                UseRandomMockDelay = false,
                MockDelaySeconds = 0.01f
            };
            var provider = new MockRewardedAdsProvider(config);

            provider.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            provider.PreloadAsync("rewarded", CancellationToken.None).GetAwaiter().GetResult();
            var result = provider.ShowAsync("rewarded", "ri_test", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(RewardedShowResult.Canceled));
        }

        [Test]
        public void ShowAsync_ReturnsFailed_WhenOutcomeFail()
        {
            var config = new RewardedAdsConfig
            {
                MockOutcome = MockAdsOutcome.Fail,
                UseRandomMockDelay = false,
                MockDelaySeconds = 0.01f
            };
            var provider = new MockRewardedAdsProvider(config);

            provider.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            provider.PreloadAsync("rewarded", CancellationToken.None).GetAwaiter().GetResult();
            var result = provider.ShowAsync("rewarded", "ri_test", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(RewardedShowResult.Failed));
        }
    }
}
