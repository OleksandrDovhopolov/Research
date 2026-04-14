using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Rewards;

namespace CardCollectionImpl
{
    public class CardCollectionRewardHandlerTests
    {
        [Test]
        public void TryHandleGroupCompleted_WhenRewardAndSpecExist_GrantsRewardAndReturnsTrue()
        {
            var grantService = new FakeRewardGrantService(grantResult: true);
            var specProvider = new FakeRewardSpecProvider(CreateSpec("group-reward"));
            var handler = CreateHandler(grantService, specProvider, ("group-a", "group-reward"));

            var result = handler.TryHandleGroupCompleted(new CardGroupCompletedData { GroupType = "group-a" }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, specProvider.TryGetCallsCount);
            Assert.AreEqual(1, grantService.GrantListCallsCount);
            Assert.AreEqual(1, grantService.LastGrantedRequests.Count);
            Assert.AreEqual("coins", grantService.LastGrantedRequests[0].RewardId);
            Assert.AreEqual(25, grantService.LastGrantedRequests[0].Amount);
        }

        [Test]
        public void TryHandleGroupCompleted_WhenGroupRewardConfigMissing_ReturnsFalseAndDoesNotGrant()
        {
            var grantService = new FakeRewardGrantService(grantResult: true);
            var specProvider = new FakeRewardSpecProvider(CreateSpec("group-reward"));
            var handler = CreateHandler(grantService, specProvider, ("another-group", "group-reward"));

            var result = handler.TryHandleGroupCompleted(new CardGroupCompletedData { GroupType = "group-a" }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.IsFalse(result);
            Assert.AreEqual(0, specProvider.TryGetCallsCount);
            Assert.AreEqual(0, grantService.GrantListCallsCount);
        }

        [Test]
        public void TryHandleGroupCompleted_WhenRewardSpecMissing_Throws()
        {
            var grantService = new FakeRewardGrantService(grantResult: true);
            var specProvider = new FakeRewardSpecProvider(null);
            var handler = CreateHandler(grantService, specProvider, ("group-a", "missing-reward"));

            var ex = Assert.Throws<Exception>(() =>
                handler.TryHandleGroupCompleted(new CardGroupCompletedData { GroupType = "group-a" }, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());

            StringAssert.Contains("Unknown reward id", ex.Message);
            Assert.AreEqual(1, specProvider.TryGetCallsCount);
            Assert.AreEqual(0, grantService.GrantListCallsCount);
        }

        [Test]
        public void TryHandleGroupCompleted_WhenGrantFails_ReturnsFalse()
        {
            var grantService = new FakeRewardGrantService(grantResult: false);
            var specProvider = new FakeRewardSpecProvider(CreateSpec("group-reward"));
            var handler = CreateHandler(grantService, specProvider, ("group-a", "group-reward"));

            var result = handler.TryHandleGroupCompleted(new CardGroupCompletedData { GroupType = "group-a" }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.IsFalse(result);
            Assert.AreEqual(1, grantService.GrantListCallsCount);
        }

        [Test]
        public void CompletedGroups_NoUiOrchestration_GrantsRewardForEachCompletedGroup()
        {
            var grantService = new FakeRewardGrantService(grantResult: true);
            var specProvider = new FakeRewardSpecProvider(
                CreateSpec("group-reward-a"),
                CreateSpec("group-reward-b"));
            var handler = CreateHandler(
                grantService,
                specProvider,
                ("group-a", "group-reward-a"),
                ("group-b", "group-reward-b"));

            var completedData = new CardGroupsCompletedData(new[]
            {
                new CardGroupCompletedData { GroupType = "group-a" },
                new CardGroupCompletedData { GroupType = "group-b" }
            });

            var allGranted = true;
            foreach (var group in completedData.Groups)
            {
                var granted = handler.TryHandleGroupCompleted(group, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                allGranted &= granted;
            }

            Assert.IsTrue(allGranted);
            Assert.AreEqual(2, specProvider.TryGetCallsCount);
            Assert.AreEqual(2, grantService.GrantListCallsCount);
        }

        [Test]
        public void TryHandleGroupCompleted_WhenTokenIsCanceled_ThrowsOperationCanceled()
        {
            var grantService = new FakeRewardGrantService(grantResult: true);
            var specProvider = new FakeRewardSpecProvider(CreateSpec("group-reward"));
            var handler = CreateHandler(grantService, specProvider, ("group-a", "group-reward"));

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                handler.TryHandleGroupCompleted(new CardGroupCompletedData { GroupType = "group-a" }, cts.Token)
                    .GetAwaiter()
                    .GetResult());

            Assert.AreEqual(0, specProvider.TryGetCallsCount);
            Assert.AreEqual(0, grantService.GrantListCallsCount);
        }

        private static CardCollectionRewardHandler CreateHandler(
            FakeRewardGrantService grantService,
            FakeRewardSpecProvider specProvider,
            params (string groupType, string rewardItemId)[] rewards)
        {
            var rewardConfigs = new List<RewardConfig>(rewards.Length);
            foreach (var reward in rewards)
            {
                rewardConfigs.Add(new RewardConfig
                {
                    rewardId = reward.groupType,
                    rewardItemId = reward.rewardItemId
                });
            }

            var staticData = new CardCollectionStaticData
            {
                EventConfig = new EventConfig
                {
                    rewards = rewardConfigs,
                    cards = new List<CardConfig>(),
                    groups = new List<CardCollectionGroupConfig>(),
                    packs = new List<CardPackConfig>(),
                    offers = new List<CardCollectionOfferConfig>()
                }
            };

            return new CardCollectionRewardHandler(staticData, specProvider, grantService);
        }

        private static RewardSpec CreateSpec(string rewardId)
        {
            return new RewardSpec
            {
                RewardId = rewardId,
                Resources = new List<RewardSpecResource>
                {
                    new() { ResourceId = "coins", Amount = 25, Category = "soft" }
                }
            };
        }

        private sealed class FakeRewardGrantService : IRewardGrantService
        {
            private readonly bool _grantResult;

            public int GrantSingleCallsCount { get; private set; }
            public int GrantListCallsCount { get; private set; }
            public List<RewardGrantRequest> LastGrantedRequests { get; private set; } = new();

            public FakeRewardGrantService(bool grantResult)
            {
                _grantResult = grantResult;
            }

            public UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                GrantSingleCallsCount++;
                LastGrantedRequests = new List<RewardGrantRequest> { rewardRequest };
                return UniTask.FromResult(_grantResult);
            }

            public UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewardRequest, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                GrantListCallsCount++;
                LastGrantedRequests = rewardRequest ?? new List<RewardGrantRequest>();
                return UniTask.FromResult(_grantResult);
            }
        }

        private sealed class FakeRewardSpecProvider : IRewardSpecProvider
        {
            private readonly Dictionary<string, RewardSpec> _specsByRewardId = new();

            public int TryGetCallsCount { get; private set; }

            public FakeRewardSpecProvider(params RewardSpec[] specs)
            {
                if (specs == null)
                {
                    return;
                }

                foreach (var spec in specs)
                {
                    if (spec == null || string.IsNullOrWhiteSpace(spec.RewardId))
                    {
                        continue;
                    }

                    _specsByRewardId[spec.RewardId] = spec;
                }
            }

            public bool TryGet(string rewardId, out RewardSpec spec)
            {
                TryGetCallsCount++;
                return _specsByRewardId.TryGetValue(rewardId, out spec);
            }
        }
    }
}
