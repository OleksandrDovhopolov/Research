using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FortuneWheel;
using Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace Rewards.Tests.Editor
{
    public sealed class AdsRewardOrchestratorTests
    {
        /*[Test]
        public void RewardedAdsRewardOrchestrator_OpensRewardWindow_OnSuccess()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildSuccess("Gems")
            };
            var intentService = new StubRewardIntentService();
            var flowService = CreateFlowService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);
            var configSo = CreateConfigSo("Gems");
            var orchestrator = new RewardedAdsRewardOrchestrator(null, flowService, rewardWindowOpener, configSo);

            var result = orchestrator.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(rewardWindowOpener.ShowCalls, Is.EqualTo(1));
            Assert.That(rewardWindowOpener.LastRewardId, Is.EqualTo("Gems"));
        }

        [Test]
        public void RewardedAdsRewardOrchestrator_DoesNotOpenRewardWindow_OnFailure()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = false
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService();
            var flowService = CreateFlowService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);
            var configSo = CreateConfigSo("Gems");
            var orchestrator = new RewardedAdsRewardOrchestrator(flowService, rewardWindowOpener, configSo);

            var result = orchestrator.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdNotReady));
            Assert.That(rewardWindowOpener.ShowCalls, Is.EqualTo(0));
        }*/

        [Test]
        public void FortuneWheelAdSpinOrchestrator_UsesAdSpinRewardId()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildSuccess(FortuneWheelConfig.Gameplay.AdSpinRewardId)
            };
            var intentService = new StubRewardIntentService();
            var flowService = CreateFlowService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);
            var orchestrator = new FortuneWheelAdSpinOrchestrator(flowService);

            var result = orchestrator.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(grantService.LastTryGrantRewardId, Is.EqualTo(FortuneWheelConfig.Gameplay.AdSpinRewardId));
        }

        [Test]
        public void FortuneWheelAdSpinOrchestrator_PropagatesFlowError()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = false
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService();
            var flowService = CreateFlowService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);
            var orchestrator = new FortuneWheelAdSpinOrchestrator(flowService);

            var result = orchestrator.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdNotReady));
        }

        private static RewardedAdsConfigSO CreateConfigSo(string rewardId)
        {
            var configSo = ScriptableObject.CreateInstance<RewardedAdsConfigSO>();
            configSo.Config = new RewardedAdsConfig
            {
                Mode = RewardedAdsMode.Mock,
                RewardId = rewardId,
                GrantTimeoutSeconds = 15,
                UseServerConfirmedGrantFlow = false,
                GrantConfirmationTimeoutSeconds = 20,
                GrantPollingIntervalSeconds = 1f,
                AndroidLevelPlayRewardedAdUnitId = "rewarded",
                IosLevelPlayRewardedAdUnitId = "rewarded",
                AndroidRewardedAdUnitId = "rewarded",
                IosRewardedAdUnitId = "rewarded"
            };
            return configSo;
        }

        private static AdsRewardFlowService CreateFlowService(
            StubRewardedAdsProvider provider,
            StubRewardGrantService grantService,
            StubRewardIntentService intentService,
            bool useServerConfirmedGrantFlow,
            StubRewardPlayerStateSyncService syncService = null)
        {
            var configSo = ScriptableObject.CreateInstance<RewardedAdsConfigSO>();
            configSo.Config = new RewardedAdsConfig
            {
                Mode = RewardedAdsMode.Mock,
                RewardId = "Gems",
                GrantTimeoutSeconds = 15,
                UseServerConfirmedGrantFlow = useServerConfirmedGrantFlow,
                GrantConfirmationTimeoutSeconds = 20,
                GrantPollingIntervalSeconds = 1f,
                AndroidLevelPlayRewardedAdUnitId = "rewarded",
                IosLevelPlayRewardedAdUnitId = "rewarded",
                AndroidRewardedAdUnitId = "rewarded",
                IosRewardedAdUnitId = "rewarded"
            };

            return new AdsRewardFlowService(
                provider,
                grantService,
                intentService,
                syncService ?? new StubRewardPlayerStateSyncService(),
                configSo);
        }

        private sealed class StubRewardedAdsProvider : IRewardedAdsProvider
        {
            public bool IsInitializedValue { get; set; }
            public bool IsReadyValue { get; set; }
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
                IsReadyValue = true;
                return UniTask.CompletedTask;
            }

            public UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                IsReadyValue = false;
                return UniTask.FromResult(ShowResult);
            }
        }

        private sealed class StubRewardGrantService : IRewardGrantService
        {
            public string LastTryGrantRewardId { get; private set; }
            public RewardGrantDetailedResult DetailedResult { get; set; } =
                RewardGrantDetailedResult.BuildSuccess("Gems");

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

            public UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                LastTryGrantRewardId = rewardId;
                return UniTask.FromResult(DetailedResult);
            }
        }

        private sealed class StubRewardIntentService : IRewardIntentService
        {
            public UniTask<CreateRewardIntentResult> CreateAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_default"
                });
            }

            public UniTask<GetRewardIntentStatusResult> GetStatusAsync(string rewardIntentId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new GetRewardIntentStatusResult
                {
                    Status = RewardIntentStatus.Fulfilled
                });
            }
        }

        private sealed class StubRewardPlayerStateSyncService : IRewardPlayerStateSyncService
        {
            public UniTask SyncFromGlobalSaveAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
