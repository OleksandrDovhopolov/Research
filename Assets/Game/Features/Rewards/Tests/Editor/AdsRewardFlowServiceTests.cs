using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
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
            var intentService = new StubRewardIntentService();
            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdNotReady));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(0));
            Assert.That(intentService.CreateCalls, Is.EqualTo(0));
        }

        [Test]
        public void TryRunFlowAsync_LegacyFlow_UsesGrantService_WhenFlagDisabled()
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
            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(1));
            Assert.That(intentService.CreateCalls, Is.EqualTo(0));
            Assert.That(provider.LastShowRewardIntentId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void TryRunFlowAsync_WithCustomRewardId_UsesCustomRewardIdInLegacyFlow()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService
            {
                DetailedResult = RewardGrantDetailedResult.BuildSuccess("fortune_wheel_ad_spin")
            };
            var intentService = new StubRewardIntentService();
            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: false);

            var result = service
                .TryRunFlowAsync("fortune_wheel_ad_spin", false, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(grantService.LastTryGrantRewardId, Is.EqualTo("fortune_wheel_ad_spin"));
        }

        [Test]
        public void TryRunFlowAsync_WithCustomRewardId_UsesCustomRewardIdInServerConfirmedFlow()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_custom"
                }
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Fulfilled });

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);
            var result = service
                .TryRunFlowAsync("fortune_wheel_ad_spin", false, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(intentService.LastCreateRewardId, Is.EqualTo("fortune_wheel_ad_spin"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsSuccess_WhenIntentFulfilled()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_123"
                }
            };
            var syncService = new StubRewardPlayerStateSyncService();
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Pending });
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Fulfilled });

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                syncService: syncService);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.Success));
            Assert.That(intentService.CreateCalls, Is.EqualTo(1));
            Assert.That(intentService.GetStatusCalls, Is.GreaterThanOrEqualTo(2));
            Assert.That(syncService.SyncCalls, Is.EqualTo(1));
            Assert.That(grantService.TryGrantDetailedCalls, Is.EqualTo(0));
            Assert.That(provider.LastShowRewardIntentId, Is.EqualTo("ri_123"));
            Assert.That(provider.PreloadCalls, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsNetworkError_WhenIntentCreateNetworkFailure()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "NETWORK_ERROR",
                    ErrorMessage = "No internet"
                }
            };

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.NetworkError));
            Assert.That(result.ErrorCode, Is.EqualTo("INTENT_CREATE_NETWORK_ERROR"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WhenIntentCreateRejected()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "REJECTED",
                    ErrorMessage = "Rejected"
                }
            };

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("INTENT_CREATE_FAILED"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsAdCanceled_WhenShowCanceled()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Canceled
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_123"
                }
            };
            var syncService = new StubRewardPlayerStateSyncService();

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                syncService: syncService);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.AdCanceled));
            Assert.That(intentService.GetStatusCalls, Is.EqualTo(0));
            Assert.That(syncService.SyncCalls, Is.EqualTo(0));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsNetworkError_WhenSaveSyncFailsWithNetworkError()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_123"
                }
            };
            var syncService = new StubRewardPlayerStateSyncService
            {
                ExceptionToThrow = new WebClientNetworkException("https://test/save/global", "No internet")
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Fulfilled });

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                syncService: syncService);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.NetworkError));
            Assert.That(result.ErrorCode, Is.EqualTo("SAVE_SYNC_NETWORK_ERROR"));
            Assert.That(syncService.SyncCalls, Is.EqualTo(1));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WhenSaveSyncFails()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult
                {
                    IsSuccess = true,
                    RewardIntentId = "ri_123"
                }
            };
            var syncService = new StubRewardPlayerStateSyncService
            {
                ExceptionToThrow = new InvalidOperationException("Bad payload")
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Fulfilled });

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                syncService: syncService);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("SAVE_SYNC_FAILED"));
            Assert.That(syncService.SyncCalls, Is.EqualTo(1));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WhenIntentRejected()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult { IsSuccess = true, RewardIntentId = "ri_123" }
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Rejected, ErrorMessage = "Rejected" });

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("REWARD_REJECTED"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WhenIntentExpired()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult { IsSuccess = true, RewardIntentId = "ri_123" }
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Expired, ErrorMessage = "Expired" });

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);
            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("REWARD_EXPIRED"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WhenConfirmationTimesOut()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult { IsSuccess = true, RewardIntentId = "ri_123" },
                AlwaysPending = true
            };

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                grantConfirmationTimeoutSeconds: 1,
                grantPollingIntervalSeconds: 0.2f);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("REWARD_CONFIRM_TIMEOUT"));
        }

        [Test]
        public void TryRunFlowAsync_ServerConfirmed_ReturnsServerFailed_WithNetworkErrorCode_WhenPollingOnlyNetworkErrors()
        {
            var provider = new StubRewardedAdsProvider
            {
                IsInitializedValue = true,
                IsReadyValue = true,
                ShowResult = RewardedShowResult.Completed
            };
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult { IsSuccess = true, RewardIntentId = "ri_123" },
                PollingException = new WebClientNetworkException("https://test", "No internet")
            };

            var service = CreateService(
                provider,
                grantService,
                intentService,
                useServerConfirmedGrantFlow: true,
                grantConfirmationTimeoutSeconds: 1,
                grantPollingIntervalSeconds: 0.2f);

            var result = service.TryRunFlowAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Type, Is.EqualTo(RewardGrantFlowResultType.ServerFailed));
            Assert.That(result.ErrorCode, Is.EqualTo("REWARD_CONFIRM_NETWORK_ERROR"));
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
            var grantService = new StubRewardGrantService();
            var intentService = new StubRewardIntentService
            {
                CreateResult = new CreateRewardIntentResult { IsSuccess = true, RewardIntentId = "ri_123" }
            };
            intentService.EnqueueStatus(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Fulfilled });

            var service = CreateService(provider, grantService, intentService, useServerConfirmedGrantFlow: true);

            var firstTask = service.TryRunFlowAsync(CancellationToken.None);
            var secondTask = service.TryRunFlowAsync(CancellationToken.None);
            var (first, second) = UniTask.WhenAll(firstTask, secondTask).GetAwaiter().GetResult();

            Assert.That(
                first.Type == RewardGrantFlowResultType.UnknownError || second.Type == RewardGrantFlowResultType.UnknownError,
                Is.True);
        }

        private static AdsRewardFlowService CreateService(
            StubRewardedAdsProvider provider,
            StubRewardGrantService grantService,
            StubRewardIntentService intentService,
            bool useServerConfirmedGrantFlow,
            int grantTimeoutSeconds = 15,
            int grantConfirmationTimeoutSeconds = 20,
            float grantPollingIntervalSeconds = 1f,
            StubRewardPlayerStateSyncService syncService = null)
        {
            var configSo = ScriptableObject.CreateInstance<RewardedAdsConfigSO>();
            configSo.Config = new RewardedAdsConfig
            {
                Mode = RewardedAdsMode.Mock,
                RewardId = "Gems",
                GrantTimeoutSeconds = grantTimeoutSeconds,
                UseServerConfirmedGrantFlow = useServerConfirmedGrantFlow,
                GrantConfirmationTimeoutSeconds = grantConfirmationTimeoutSeconds,
                GrantPollingIntervalSeconds = grantPollingIntervalSeconds,
                AndroidRewardedAdUnitId = "rewarded",
                IosRewardedAdUnitId = "rewarded"
            };

            return new AdsRewardFlowService(
                null,
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
            public int PreloadCalls { get; private set; }
            public int ShowDelayMs { get; set; }
            public RewardedShowResult ShowResult { get; set; } = RewardedShowResult.Completed;
            public string LastShowRewardIntentId { get; private set; }

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

            public async UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                LastShowRewardIntentId = rewardIntentId;
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

            public async UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                TryGrantDetailedCalls++;
                LastTryGrantRewardId = rewardId;
                if (DelayMs > 0)
                {
                    await UniTask.Delay(TimeSpan.FromMilliseconds(DelayMs), cancellationToken: ct);
                }

                return DetailedResult;
            }
        }

        private sealed class StubRewardIntentService : IRewardIntentService
        {
            private readonly Queue<GetRewardIntentStatusResult> _statuses = new();

            public int CreateCalls { get; private set; }
            public int GetStatusCalls { get; private set; }
            public string LastCreateRewardId { get; private set; }
            public bool AlwaysPending { get; set; }
            public Exception PollingException { get; set; }
            public CreateRewardIntentResult CreateResult { get; set; } = new()
            {
                IsSuccess = true,
                RewardIntentId = "ri_default"
            };

            public UniTask<CreateRewardIntentResult> CreateAsync(string rewardId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                CreateCalls++;
                LastCreateRewardId = rewardId;
                return UniTask.FromResult(CreateResult);
            }

            public UniTask<GetRewardIntentStatusResult> GetStatusAsync(string rewardIntentId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                GetStatusCalls++;

                if (PollingException != null)
                {
                    throw PollingException;
                }

                if (_statuses.Count > 0)
                {
                    return UniTask.FromResult(_statuses.Dequeue());
                }

                if (AlwaysPending)
                {
                    return UniTask.FromResult(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Pending });
                }

                return UniTask.FromResult(new GetRewardIntentStatusResult { Status = RewardIntentStatus.Unknown });
            }

            public void EnqueueStatus(GetRewardIntentStatusResult status)
            {
                _statuses.Enqueue(status);
            }
        }

        private sealed class StubRewardPlayerStateSyncService : IRewardPlayerStateSyncService
        {
            public int SyncCalls { get; private set; }
            public Exception ExceptionToThrow { get; set; }

            public UniTask SyncFromGlobalSaveAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                SyncCalls++;
                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                return UniTask.CompletedTask;
            }
        }
    }
}
