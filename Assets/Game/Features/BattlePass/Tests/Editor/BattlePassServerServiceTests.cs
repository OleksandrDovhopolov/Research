using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;
using Rewards;
using UnityEngine;
using UnityEngine.TestTools;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassServerServiceTests
    {
        [Test]
        public void GetCurrentAsync_UsesEncodedPlayerId_AndMapsNewRewardIdContract()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) =>
                {
                    Assert.That(url, Is.EqualTo("battle-pass/current?playerId=player%201"));

                    return new
                    {
                        season = new
                        {
                            id = "season_2026_s1",
                            title = "Season 1",
                            startAtUtc = "2026-05-01T00:00:00Z",
                            endAtUtc = "2026-06-01T00:00:00Z",
                            maxLevel = 50,
                            status = "active",
                            configVersion = "v1"
                        },
                        products = new
                        {
                            premiumProductId = "battle_pass_premium_2026_s1",
                            platinumProductId = "battle_pass_platinum_2026_s1"
                        },
                        userState = new
                        {
                            seasonId = "season_2026_s1",
                            level = 12,
                            xp = 340,
                            passType = "premium",
                            claimedRewards = new object[]
                            {
                                new
                                {
                                    level = 2,
                                    rewardTrack = "default",
                                    claimedAtUtc = "2026-04-26T10:00:00Z"
                                }
                            },
                            claimableRewards = new object[]
                            {
                                new
                                {
                                    level = 2,
                                    rewardTrack = "premium",
                                    rewardId = "reward_premium_1"
                                }
                            }
                        },
                        levels = new object[]
                        {
                            new
                            {
                                level = 2,
                                xpRequired = 100,
                                defaultRewardId = "reward_default_1",
                                premiumRewardId = "reward_premium_1"
                            }
                        },
                        serverTimeUtc = "2026-04-24T10:00:00Z"
                    };
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player 1"));

            var snapshot = service.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Season, Is.Not.Null);
            Assert.That(snapshot.Season.Title, Is.EqualTo("Season 1"));
            Assert.That(snapshot.Products.PremiumProductId, Is.EqualTo("battle_pass_premium_2026_s1"));
            Assert.That(snapshot.Products.PlatinumProductId, Is.EqualTo("battle_pass_platinum_2026_s1"));
            Assert.That(snapshot.UserState.PassType, Is.EqualTo(BattlePassPassType.Premium));
            Assert.That(snapshot.UserState.ClaimedRewards.Count, Is.EqualTo(1));
            Assert.That(snapshot.UserState.ClaimedRewards[0].Level, Is.EqualTo(2));
            Assert.That(snapshot.UserState.ClaimedRewards[0].RewardTrack, Is.EqualTo(BattlePassRewardTrack.Default));
            Assert.That(snapshot.UserState.ClaimedRewards[0].ClaimedAtUtc, Is.EqualTo(DateTimeOffset.Parse("2026-04-26T10:00:00Z")));
            Assert.That(snapshot.UserState.ClaimableRewards.Count, Is.EqualTo(1));
            Assert.That(snapshot.UserState.ClaimableRewards[0].Level, Is.EqualTo(2));
            Assert.That(snapshot.UserState.ClaimableRewards[0].RewardTrack, Is.EqualTo(BattlePassRewardTrack.Premium));
            Assert.That(snapshot.UserState.ClaimableRewards[0].RewardId, Is.EqualTo("reward_premium_1"));
            Assert.That(snapshot.Levels.Count, Is.EqualTo(1));
            Assert.That(snapshot.Levels[0].DefaultReward.RewardId, Is.EqualTo("reward_default_1"));
            Assert.That(snapshot.Levels[0].PremiumReward.RewardId, Is.EqualTo("reward_premium_1"));
            Assert.That(snapshot.ServerTimeUtc, Is.EqualTo(DateTimeOffset.Parse("2026-04-24T10:00:00Z")));
        }

        [Test]
        public void GetCurrentAsync_FallsBackToLegacyGoldProductId_AndLegacyRewardArrays()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (_, _) => new
                {
                    season = new
                    {
                        id = "season_2026_s1",
                        title = "Season 1",
                        startAtUtc = "2026-05-01T00:00:00Z",
                        endAtUtc = "2026-06-01T00:00:00Z",
                        maxLevel = 50,
                        status = "active",
                        configVersion = "v1"
                    },
                    products = new
                    {
                        goldProductId = "battle_pass_gold_2026_s1",
                        premiumProductId = "",
                        platinumProductId = "battle_pass_platinum_2026_s1"
                    },
                    userState = new
                    {
                        seasonId = "season_2026_s1",
                        level = 12,
                        xp = 340,
                        passType = "gold",
                        claimedRewards = Array.Empty<object>(),
                        claimableRewards = Array.Empty<object>()
                    },
                    levels = new object[]
                    {
                        new
                        {
                            level = 2,
                            xpRequired = 100,
                            defaultRewards = new object[] { "reward_default_1" },
                            premiumRewards = new object[] { "reward_premium_1" }
                        }
                    },
                    serverTimeUtc = "2026-04-24T10:00:00Z"
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player-1"));

            var snapshot = service.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshot.Products.PremiumProductId, Is.EqualTo("battle_pass_gold_2026_s1"));
            Assert.That(snapshot.UserState.PassType, Is.EqualTo(BattlePassPassType.Premium));
            Assert.That(snapshot.Levels[0].DefaultReward.RewardId, Is.EqualTo("reward_default_1"));
            Assert.That(snapshot.Levels[0].PremiumReward.RewardId, Is.EqualTo("reward_premium_1"));
        }

        [Test]
        public void GetCurrentAsync_SkipsUnsupportedRewardTracksInClaimState()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (_, _) => new
                {
                    season = new
                    {
                        id = "season_2026_s1",
                        title = "Season 1",
                        startAtUtc = "2026-05-01T00:00:00Z",
                        endAtUtc = "2026-06-01T00:00:00Z",
                        maxLevel = 50,
                        status = "active",
                        configVersion = "v1"
                    },
                    products = new
                    {
                        premiumProductId = "battle_pass_premium_2026_s1",
                        platinumProductId = "battle_pass_platinum_2026_s1"
                    },
                    userState = new
                    {
                        seasonId = "season_2026_s1",
                        level = 12,
                        xp = 340,
                        passType = "premium",
                        claimedRewards = new object[]
                        {
                            new
                            {
                                level = 2,
                                rewardTrack = "platinum",
                                claimedAtUtc = "2026-04-26T10:00:00Z"
                            }
                        },
                        claimableRewards = new object[]
                        {
                            new
                            {
                                level = 2,
                                rewardTrack = "platinum",
                                rewardId = "reward_platinum_1"
                            }
                        }
                    },
                    levels = Array.Empty<object>(),
                    serverTimeUtc = "2026-04-24T10:00:00Z"
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player-1"));

            LogAssert.Expect(
                LogType.Error,
                "[BattlePassServerService] Claimed reward cell has unsupported rewardTrack 'platinum' and was skipped.");
            LogAssert.Expect(
                LogType.Error,
                "[BattlePassServerService] Claimable reward cell has unsupported rewardTrack 'platinum' and was skipped.");
            var snapshot = service.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshot.UserState.ClaimedRewards, Is.Empty);
            Assert.That(snapshot.UserState.ClaimableRewards, Is.Empty);
        }

        [Test]
        public void GetCurrentAsync_ReturnsUnavailableSnapshot_WhenSeasonMissing()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) => new
                {
                    season = (object)null,
                    products = (object)null,
                    userState = (object)null,
                    levels = Array.Empty<object>(),
                    serverTimeUtc = "2026-04-24T10:00:00Z"
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player-1"));

            var snapshot = service.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Season, Is.Null);
            Assert.That(snapshot.Products, Is.Null);
            Assert.That(snapshot.UserState, Is.Null);
            Assert.That(snapshot.Levels, Is.Empty);
        }

        [Test]
        public void AddXpAsync_PostsPlayerIdAndAmount_AndMapsUpdatedState()
        {
            var webClient = new StubWebClient
            {
                PostResponder = (url, request, responseType) =>
                {
                    Assert.That(url, Is.EqualTo("battle-pass/xp/add"));
                    Assert.That(Newtonsoft.Json.Linq.JObject.FromObject(request)["playerId"]?.ToObject<string>(), Is.EqualTo("player-1"));
                    Assert.That(Newtonsoft.Json.Linq.JObject.FromObject(request)["amount"]?.ToObject<int>(), Is.EqualTo(20));

                    return new
                    {
                        seasonId = "season_2026_s1",
                        level = 13,
                        xp = 360,
                        passType = "premium"
                    };
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player-1"));

            var state = service.AddXpAsync(20, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(state.SeasonId, Is.EqualTo("season_2026_s1"));
            Assert.That(state.Level, Is.EqualTo(13));
            Assert.That(state.Xp, Is.EqualTo(360));
            Assert.That(state.PassType, Is.EqualTo(BattlePassPassType.Premium));
        }

        private sealed class StubPlayerIdentityProvider : IPlayerIdentityProvider
        {
            private readonly string _playerId;

            public StubPlayerIdentityProvider(string playerId)
            {
                _playerId = playerId;
            }

            public string GetPlayerId()
            {
                return _playerId;
            }
        }

        private sealed class StubWebClient : IWebClient
        {
            public Func<string, Type, object> GetResponder { get; set; }
            public Func<string, object, Type, object> PostResponder { get; set; }

            public UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                if (GetResponder == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                var response = GetResponder(url, typeof(TResponse));
                if (response == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                if (response is TResponse typedResponse)
                {
                    return UniTask.FromResult(typedResponse);
                }

                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                var converted = Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>(serialized);
                return UniTask.FromResult(converted);
            }

            public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (PostResponder == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                var response = PostResponder(url, data, typeof(TResponse));
                if (response == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                if (response is TResponse typedResponse)
                {
                    return UniTask.FromResult(typedResponse);
                }

                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                var converted = Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>(serialized);
                return UniTask.FromResult(converted);
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
