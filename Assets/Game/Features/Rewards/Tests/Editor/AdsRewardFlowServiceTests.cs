using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Rewards.Tests.Editor
{
    public sealed class AdsRewardFlowServiceTests
    {
        [Test]
        public void TryRunFlowAsync_ReturnsAdNotReady_WhenAdIsNotReady()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = false
            };
            var grantService = new StubRewardGrantService();
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdNotReady));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(0));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsSuccess_WhenShowCompletedAndGrantSucceeded()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildSuccess("Gems", 777)
            };
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(result.NewCrystalsBalance, Is.EqualTo(777));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(1));
            Assert.That(provider.PreloadCalls, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsServerFailed_WhenGrantRejected()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildFailure(
                    "Gems",
                    RewardGrantFailureType.Rejected,
                    "REJECTED",
                    "Rejected.")
            };
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("REJECTED"));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsNetworkError_WhenGrantHasNetworkFailure()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildFailure(
                    "Gems",
                    RewardGrantFailureType.Network,
                    "NETWORK",
                    "Network.")
            };
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.NetworkError));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsAdCanceled_WhenShowCanceled()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Canceled
            };
            var grantService = new StubRewardGrantService();
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdCanceled));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(0));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsAdFailed_WhenShowFailed()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Failed
            };
            var grantService = new StubRewardGrantService();
            var service = CreateService(provider, grantService);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdFailed));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(0));
        }

        [Test]
        public void TryRunFlowAsync_ReturnsNetworkError_WhenGrantRequestTimesOut()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DelayMs = 1500
            };
            var service = CreateService(provider, grantService, grantTimeoutSeconds: 1);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.NetworkError));
            Assert.That(result.ErrorCode, Is.EqualTo("TIMEOUT"));
        }

        [Test]
        public void TryRunFlowAsync_DoesNotRunTwice_WhenCalledInParallel()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed,
                ShowDelayMs = 100
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildSuccess("Gems", 100)
            };
            var service = CreateService(provider, grantService);

            var firstTask = service.TryRunFlowAsync(CancellationToken.None);
            var secondTask = service.TryRunFlowAsync(CancellationToken.None);
            var (first, second) = UniTask.WhenAll(firstTask, secondTask).GetAwaiter().GetResult();

            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(1));
            Assert.That(
                first.Type == RewardGrantFlowResultType.UnknownError || second.Type == RewardGrantFlowResultType.UnknownError,
                Is.True);
        }

        private static AdsRewardFlowService CreateService(
            StubRewardedAdsProvider provider,
            StubRewardGrantService grantService,
            int grantTimeoutSeconds = 15)
        {
            var configSo = ScriptableObject.CreateInstance<RewardedAdsConfigSO>();
            configSo.Config = new RewardedAdsConfig
            {
                Mode = RewardedAdsMode.Mock,
                RewardId = "Gems",
                GrantTimeoutSeconds = grantTimeoutSeconds,
                AndroidRewardedAdUnitId = "rewarded",
                IosRewardedAdUnitId = "rewarded"
            };

            return new AdsRewardFlowService(provider, grantService, configSo);
        }

        private sealed class StubRewardedAdsProvider : IRewardedAdsProvider
        {
            public bool IsInitializedValue { get; set; }
            public bool IsReadyValue { get; set; }
            public int PreloadCalls { get; private set; }
            public int ShowDelayMs { get; set; }
            public RewardedShowResult ShowResult { get; set; } = RewardedShowResult.Completed;

            public bool IsInitialized => IsInitializedValue;

            public bool IsAdReady(string adUnitId)
            {
                return IsReadyValue;
            }

            public UniTask InitializeAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                IsInitializedValue = true;
                return UniTask.CompletedTask;
            }

            public UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                PreloadCalls++;
                IsReadyValue = true;
                return UniTask.CompletedTask;
            }

            public async UniTask<RewardedShowResult> ShowAsync(string adUnitId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (ShowDelayMs > 0)
                {
                    await UniTask.Delay(TimeSpan.FromMilliseconds(ShowDelayMs), cancellationToken: ct);
                }

                IsReadyValue = false;
                return ShowResult;
            }
        }

        private sealed class StubRewardGrantService : IRewardGrantService
        {
            public int DelayMs { get; set; }
            public int TryGrantDetailedCalls { get; private set; }
            public RewardGrantDetailedResult DetailedResult { get; set; } =
                RewardGrantDetailedResult.BuildSuccess("Gems", 50);

            public UniTask<bool> TryGrantAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(DetailedResult.Success);
            }

            public UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewards, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(false);
            }

            public async UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                TryGrantDetailedCalls++;
                if (DelayMs > 0)
                {
                    await UniTask.Delay(TimeSpan.FromMilliseconds(DelayMs), cancellationToken: ct);
                }

                return DetailedResult;
            }

            public UniTask<bool> TryApplyGrantResponseAsync(GrantRewardResponse grantResponse, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(DetailedResult.Success);
            }
        }
    }
}
