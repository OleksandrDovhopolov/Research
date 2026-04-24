using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rewards;
using UnityEngine;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassUiModelFactoryTests
    {
        [Test]
        public void Create_BuildsTwoSeparateTracks_AndSkipsUnknownRewards()
        {
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
                ["reward_default"] = CreateRewardSpec("reward_default", 10),
                ["reward_premium"] = CreateRewardSpec("reward_premium", 25)
            });
            var factory = new BattlePassUiModelFactory(rewardSpecProvider);
            var snapshot = new BattlePassSnapshot(
                new BattlePassSeason(
                    "season_1",
                    "Season 1",
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    50,
                    "active",
                    "v1"),
                new BattlePassProducts("premium_sku", "platinum_sku"),
                new BattlePassUserState("season_1", 4, 120, BattlePassPassType.Premium),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        new[] { new BattlePassRewardRef("reward_default") },
                        new[]
                        {
                            new BattlePassRewardRef("reward_premium"),
                            new BattlePassRewardRef("unknown_reward")
                        })
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel, Is.Not.Null);
            Assert.That(uiModel.Title, Is.EqualTo("Season 1"));
            Assert.That(uiModel.DefaultTrackLevels.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumTrackLevels.Count, Is.EqualTo(1));
            Assert.That(uiModel.DefaultTrackLevels[0].Rewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.DefaultTrackLevels[0].Rewards[0].RewardId, Is.EqualTo("reward_default"));
            Assert.That(uiModel.PremiumTrackLevels[0].Rewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumTrackLevels[0].Rewards[0].RewardId, Is.EqualTo("reward_premium"));
        }

        private static RewardSpec CreateRewardSpec(string rewardId, int amount)
        {
            return new RewardSpec
            {
                RewardId = rewardId,
                Icon = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero),
                TotalAmountForUi = amount,
                Resources = new List<RewardSpecResource>()
            };
        }

        private sealed class StubRewardSpecProvider : IRewardSpecProvider
        {
            private readonly Dictionary<string, RewardSpec> _specs;

            public StubRewardSpecProvider(Dictionary<string, RewardSpec> specs)
            {
                _specs = specs;
            }

            public bool TryGet(string rewardId, out RewardSpec spec)
            {
                return _specs.TryGetValue(rewardId, out spec);
            }
        }
    }
}
