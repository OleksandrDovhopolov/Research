using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;
using Rewards;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassServerServiceTests
    {
        [Test]
        public void GetCurrentAsync_UsesEncodedPlayerId_AndMapsGoldToPremium()
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
                            goldProductId = "battle_pass_gold_2026_s1",
                            platinumProductId = "battle_pass_platinum_2026_s1"
                        },
                        userState = new
                        {
                            seasonId = "season_2026_s1",
                            level = 12,
                            xp = 340,
                            passType = "gold"
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
                    };
                }
            };

            var service = new BattlePassServerService(webClient, new StubPlayerIdentityProvider("player 1"));

            var snapshot = service.GetCurrentAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Season, Is.Not.Null);
            Assert.That(snapshot.Season.Title, Is.EqualTo("Season 1"));
            Assert.That(snapshot.Products.PremiumProductId, Is.EqualTo("battle_pass_gold_2026_s1"));
            Assert.That(snapshot.Products.PlatinumProductId, Is.EqualTo("battle_pass_platinum_2026_s1"));
            Assert.That(snapshot.UserState.PassType, Is.EqualTo(BattlePassPassType.Premium));
            Assert.That(snapshot.Levels.Count, Is.EqualTo(1));
            Assert.That(snapshot.Levels[0].DefaultRewards[0].RewardId, Is.EqualTo("reward_default_1"));
            Assert.That(snapshot.Levels[0].PremiumRewards[0].RewardId, Is.EqualTo("reward_premium_1"));
            Assert.That(snapshot.ServerTimeUtc, Is.EqualTo(DateTimeOffset.Parse("2026-04-24T10:00:00Z")));
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
                return UniTask.FromResult(default(TResponse));
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
