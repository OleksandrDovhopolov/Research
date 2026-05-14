using System.Collections.Generic;
using CoreResources;
using NUnit.Framework;
using Rewards;
using UnityEngine;
using UnityEngine.TestTools;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassOptimisticRewardApplierTests
    {
        [Test]
        public void Apply_AggregatesResourceRewards_AndIgnoresInventoryItems()
        {
            var resourceManager = new ResourceManager(null);
            var resourceApplyService = new ResourceManagerOptimisticResourceApplyService(resourceManager);
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
                ["reward_1"] = new()
                {
                    RewardId = "reward_1",
                    Resources = new List<RewardSpecResource>
                    {
                        new() { Kind = RewardKind.Resource, ResourceId = "Gold", Amount = 10 },
                        new() { Kind = RewardKind.Resource, ResourceId = "Gold", Amount = 5 },
                        new() { Kind = RewardKind.InventoryItem, ResourceId = "pack_a", Amount = 1, Category = "card_pack" },
                        new() { Kind = RewardKind.Resource, ResourceId = "Energy", Amount = 3 }
                    }
                },
                ["reward_2"] = new()
                {
                    RewardId = "reward_2",
                    Resources = new List<RewardSpecResource>
                    {
                        new() { Kind = RewardKind.Resource, ResourceId = "Gold", Amount = 7 }
                    }
                }
            });
            var applier = new BattlePassOptimisticRewardApplier(rewardSpecProvider, resourceApplyService);

            applier.Apply(new[]
            {
                new BattlePassGrantedRewardCell(1, BattlePassRewardTrack.Default, "reward_1"),
                new BattlePassGrantedRewardCell(2, BattlePassRewardTrack.Premium, "reward_2")
            });

            Assert.That(resourceManager.Get(ResourceType.Gold), Is.EqualTo(22));
            Assert.That(resourceManager.Get(ResourceType.Energy), Is.EqualTo(3));
            Assert.That(resourceManager.Get(ResourceType.Gems), Is.EqualTo(0));
        }

        [Test]
        public void Apply_LogsAndSkipsUnknownRewardId()
        {
            var resourceManager = new ResourceManager(null);
            var resourceApplyService = new ResourceManagerOptimisticResourceApplyService(resourceManager);
            var applier = new BattlePassOptimisticRewardApplier(new StubRewardSpecProvider(), resourceApplyService);

            LogAssert.Expect(
                LogType.Error,
                "[BattlePassOptimisticRewardApplier] Unknown rewardId 'unknown_reward' was skipped.");
            applier.Apply(new[]
            {
                new BattlePassGrantedRewardCell(1, BattlePassRewardTrack.Default, "unknown_reward")
            });

            Assert.That(resourceManager.Get(ResourceType.Gold), Is.EqualTo(0));
            Assert.That(resourceManager.Get(ResourceType.Energy), Is.EqualTo(0));
            Assert.That(resourceManager.Get(ResourceType.Gems), Is.EqualTo(0));
        }

        [Test]
        public void Apply_LogsAndSkipsRewardSpecWithoutResources()
        {
            var resourceManager = new ResourceManager(null);
            var resourceApplyService = new ResourceManagerOptimisticResourceApplyService(resourceManager);
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
                ["reward_empty"] = new()
                {
                    RewardId = "reward_empty",
                    Resources = new List<RewardSpecResource>()
                }
            });
            var applier = new BattlePassOptimisticRewardApplier(rewardSpecProvider, resourceApplyService);

            LogAssert.Expect(
                LogType.Error,
                "[BattlePassOptimisticRewardApplier] Reward spec 'reward_empty' has no resources and was skipped.");
            applier.Apply(new[]
            {
                new BattlePassGrantedRewardCell(1, BattlePassRewardTrack.Default, "reward_empty")
            });

            Assert.That(resourceManager.Get(ResourceType.Gold), Is.EqualTo(0));
        }

        private sealed class StubRewardSpecProvider : IRewardSpecProvider, IBattlePassRewardCatalog
        {
            private readonly IReadOnlyDictionary<string, RewardSpec> _rewardSpecs;

            public StubRewardSpecProvider(IReadOnlyDictionary<string, RewardSpec> rewardSpecs = null)
            {
                _rewardSpecs = rewardSpecs ?? new Dictionary<string, RewardSpec>();
            }

            public bool TryGet(string rewardId, out RewardSpec spec)
            {
                return _rewardSpecs.TryGetValue(rewardId, out spec);
            }

            public bool TryGet(string rewardId, out BattlePassRewardDefinition rewardDefinition)
            {
                rewardDefinition = null;
                if (!_rewardSpecs.TryGetValue(rewardId, out var spec) || spec == null)
                {
                    return false;
                }

                var resourceDeltas = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                if (spec.Resources != null)
                {
                    for (var i = 0; i < spec.Resources.Count; i++)
                    {
                        var resource = spec.Resources[i];
                        if (resource == null ||
                            resource.Kind != RewardKind.Resource ||
                            resource.Amount <= 0 ||
                            string.IsNullOrWhiteSpace(resource.ResourceId))
                        {
                            continue;
                        }

                        resourceDeltas.TryGetValue(resource.ResourceId, out var currentAmount);
                        resourceDeltas[resource.ResourceId] = currentAmount + resource.Amount;
                    }
                }

                rewardDefinition = new BattlePassRewardDefinition(
                    rewardId,
                    spec.TotalAmountForUi,
                    resourceDeltas);
                return true;
            }
        }
    }
}
