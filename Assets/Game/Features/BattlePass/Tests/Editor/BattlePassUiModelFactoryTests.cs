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
        public void Create_BuildsTwoSeparateFlatRewardLists_AndSkipsUnknownRewards()
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
                new BattlePassUserState(
                    "season_1",
                    4,
                    120,
                    BattlePassPassType.Premium,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        new BattlePassRewardRef("reward_default"),
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel, Is.Not.Null);
            Assert.That(uiModel.Title, Is.EqualTo("Season 1"));
            Assert.That(uiModel.DefaultRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.DefaultRewards[0].RewardId, Is.EqualTo("reward_default"));
            Assert.That(uiModel.DefaultRewards[0].Level, Is.EqualTo(1));
            Assert.That(uiModel.DefaultRewards[0].RewardTrack, Is.EqualTo(BattlePassRewardTrack.Default));
            Assert.That(uiModel.DefaultRewards[0].IsPremiumTrack, Is.False);
            Assert.That(uiModel.DefaultRewards[0].IsClaimed, Is.False);
            Assert.That(uiModel.DefaultRewards[0].IsLocked, Is.False);
            Assert.That(uiModel.PremiumRewards[0].RewardId, Is.EqualTo("reward_premium"));
            Assert.That(uiModel.PremiumRewards[0].Level, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards[0].RewardTrack, Is.EqualTo(BattlePassRewardTrack.Premium));
            Assert.That(uiModel.PremiumRewards[0].IsPremiumTrack, Is.True);
            Assert.That(uiModel.PremiumRewards[0].IsLocked, Is.False);
        }

        [Test]
        public void Create_WhenRewardCellIsClaimed_MarksOnlyMatchingTrackAsClaimed()
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
                new BattlePassUserState(
                    "season_1",
                    4,
                    120,
                    BattlePassPassType.Premium,
                    new[]
                    {
                        new BattlePassClaimedRewardCell(
                            1,
                            BattlePassRewardTrack.Default,
                            DateTimeOffset.Parse("2026-04-26T10:00:00Z"))
                    },
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        new BattlePassRewardRef("reward_default"),
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel.DefaultRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.DefaultRewards[0].IsClaimed, Is.True);
            Assert.That(uiModel.DefaultRewards[0].IsClaimable, Is.False);
            Assert.That(uiModel.DefaultRewards[0].IsLocked, Is.False);
            Assert.That(uiModel.PremiumRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards[0].IsClaimed, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsClaimable, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsLocked, Is.False);
        }

        [Test]
        public void Create_WhenUserHasNoPremiumAccess_MarksPremiumRewardsAsLocked()
        {
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
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
                new BattlePassUserState(
                    "season_1",
                    4,
                    120,
                    BattlePassPassType.None,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        null,
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel.PremiumRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards[0].IsClaimed, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsClaimable, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsLocked, Is.True);
        }

        [Test]
        public void Create_WhenRewardCellIsClaimable_MarksCellAsClaimable()
        {
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
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
                new BattlePassUserState(
                    "season_1",
                    4,
                    120,
                    BattlePassPassType.Premium,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    new[]
                    {
                        new BattlePassClaimableRewardCell(1, BattlePassRewardTrack.Premium, "reward_premium")
                    }),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        null,
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel.PremiumRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards[0].IsClaimed, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsClaimable, Is.True);
            Assert.That(uiModel.PremiumRewards[0].IsLocked, Is.False);
        }

        [Test]
        public void Create_WhenRewardIsClaimed_ClaimedStatusHasPriorityOverClaimableAndLocked()
        {
            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
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
                new BattlePassUserState(
                    "season_1",
                    4,
                    120,
                    BattlePassPassType.None,
                    new[]
                    {
                        new BattlePassClaimedRewardCell(
                            1,
                            BattlePassRewardTrack.Premium,
                            DateTimeOffset.Parse("2026-04-26T10:00:00Z"))
                    },
                    new[]
                    {
                        new BattlePassClaimableRewardCell(1, BattlePassRewardTrack.Premium, "reward_premium")
                    }),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        null,
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));

            var uiModel = factory.Create(snapshot);

            Assert.That(uiModel.PremiumRewards.Count, Is.EqualTo(1));
            Assert.That(uiModel.PremiumRewards[0].IsClaimed, Is.True);
            Assert.That(uiModel.PremiumRewards[0].IsClaimable, Is.False);
            Assert.That(uiModel.PremiumRewards[0].IsLocked, Is.False);
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
