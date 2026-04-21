using System;
using System.Collections.Generic;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace Rewards.Tests.Editor
{
    public sealed class WebClientBackedServicesTests
    {
        [Test]
        public void ServerRewardGrantService_TryGrantAsync_AppliesSnapshot_WhenResponseSuccessful()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                PostResponder = (url, requestType, responseType, request) =>
                {
                    Assert.That(url, Is.EqualTo("rewards/grant"));
                    return new GrantRewardResponse
                    {
                        Success = true,
                        RewardId = "reward_a",
                        PlayerState = new PlayerStateSnapshotDto
                        {
                            Resources = new Dictionary<string, int> { { "gold", 100 } }
                        }
                    };
                }
            };
            var service = new ServerRewardGrantService(new StubPlayerIdentityProvider("player-1"), webClient, new[] { snapshotHandler });

            var result = service.TryGrantAsync("reward_a", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.True);
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
        }

        [Test]
        public void ServerRewardGrantService_TryGrantAsync_ReturnsFalse_WhenResponseRejected()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                PostResponder = (url, requestType, responseType, request) =>
                {
                    return new GrantRewardResponse
                    {
                        Success = false,
                        RewardId = "reward_a",
                        ErrorCode = "ERR"
                    };
                }
            };
            var service = new ServerRewardGrantService(new StubPlayerIdentityProvider("player-1"), webClient, new[] { snapshotHandler });

            var result = service.TryGrantAsync("reward_a", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.False);
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void ServerRewardGrantService_TryGrantDetailedAsync_ReturnsBalance_WhenResponseSuccessful()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                PostResponder = (url, requestType, responseType, request) =>
                {
                    return new GrantRewardResponse
                    {
                        Success = true,
                        RewardId = "Gems",
                        PlayerState = new PlayerStateSnapshotDto
                        {
                            Resources = new Dictionary<string, int> { { "Gems", 456 } }
                        }
                    };
                }
            };
            var service = new ServerRewardGrantService(new StubPlayerIdentityProvider("player-1"), webClient, new[] { snapshotHandler });

            var result = service.TryGrantDetailedAsync("Gems", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.True);
            Assert.That(result.NewCrystalsBalance, Is.EqualTo(456));
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
        }

        [Test]
        public void ServerRewardGrantService_TryGrantDetailedAsync_ReturnsRejected_WhenResponseRejected()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                PostResponder = (url, requestType, responseType, request) =>
                {
                    return new GrantRewardResponse
                    {
                        Success = false,
                        RewardId = "Gems",
                        ErrorCode = "REJECTED",
                        ErrorMessage = "No reward."
                    };
                }
            };
            var service = new ServerRewardGrantService(new StubPlayerIdentityProvider("player-1"), webClient, new[] { snapshotHandler });

            var result = service.TryGrantDetailedAsync("Gems", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureType, Is.EqualTo(RewardGrantFailureType.Rejected));
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void ServerRewardGrantService_TryGrantDetailedAsync_ReturnsNetworkFailure_WhenNetworkExceptionThrown()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                PostException = new WebClientNetworkException("https://test/rewards/grant", "No internet")
            };
            var service = new ServerRewardGrantService(new StubPlayerIdentityProvider("player-1"), webClient, new[] { snapshotHandler });

            var result = service.TryGrantDetailedAsync("Gems", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureType, Is.EqualTo(RewardGrantFailureType.Network));
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void UnityWebRequestResourceAdjustApi_AdjustAsync_MapsResponse()
        {
            var webClient = new StubWebClient
            {
                PostResponder = (url, requestType, responseType, request) =>
                {
                    Assert.That(url, Is.EqualTo("resources/adjust"));
                    return new
                    {
                        success = true,
                        errorCode = (string)null,
                        errorMessage = (string)null,
                        resources = new { gold = 10, energy = 20, gems = 30 }
                    };
                }
            };
            var api = new UnityWebRequestResourceAdjustApi(webClient);

            var response = api.AdjustAsync(new AdjustResourceCommand
            {
                PlayerId = "player-1",
                ResourceId = "gold",
                Delta = 5,
                Reason = "test"
            }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(response.Success, Is.True);
            Assert.That(response.Resources, Is.Not.Null);
            Assert.That(response.Resources.Gold, Is.EqualTo(10));
            Assert.That(response.Resources.Energy, Is.EqualTo(20));
            Assert.That(response.Resources.Gems, Is.EqualTo(30));
        }

        [Test]
        public void ServerRewardIntentService_GetStatusAsync_MapsResponseWithoutBalance()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) =>
                {
                    Assert.That(url, Is.EqualTo("rewards/intent/status?rewardIntentId=ri_123"));
                    return new
                    {
                        status = "fulfilled",
                        errorCode = (string)null,
                        errorMessage = (string)null
                    };
                }
            };
            var service = new ServerRewardIntentService(new StubPlayerIdentityProvider("player-1"), webClient);

            var result = service.GetStatusAsync("ri_123", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(RewardIntentStatus.Fulfilled));
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void ServerRewardPlayerStateSyncService_SyncFromGlobalSaveAsync_AppliesSnapshot_WhenRawPayload()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) =>
                {
                    Assert.That(url, Is.EqualTo("save/global?playerId=player-1"));
                    return JObject.Parse(
                        "{ \"Resources\": { \"Gold\": 100, \"Energy\": 150, \"Gems\": 2000 }, " +
                        "\"Inventory\": { \"InventoryItems\": { \"Emerald_Pack\": 1, \"Lazurite_Pack\": { \"amount\": 2 } } } }");
                }
            };
            var service = new ServerRewardPlayerStateSyncService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                new[] { snapshotHandler });

            service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot, Is.Not.Null);
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gold"], Is.EqualTo(100));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Energy"], Is.EqualTo(150));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gems"], Is.EqualTo(2000));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems.Count, Is.EqualTo(2));
        }

        [Test]
        public void ServerRewardPlayerStateSyncService_SyncFromGlobalSaveAsync_AppliesSnapshot_WhenEnvelopeDataString()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) =>
                {
                    var payloadObject = new JObject
                    {
                        ["resources"] = new JObject
                        {
                            ["Gold"] = 7,
                            ["Energy"] = 8,
                            ["Gems"] = 9
                        },
                        ["inventory"] = new JObject
                        {
                            ["inventoryItems"] = new JObject
                            {
                                ["Sapphire_Pack"] = 3
                            }
                        }
                    };

                    return new JObject
                    {
                        ["success"] = true,
                        ["data"] = payloadObject.ToString(Newtonsoft.Json.Formatting.None)
                    };
                }
            };
            var service = new ServerRewardPlayerStateSyncService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                new[] { snapshotHandler });

            service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gold"], Is.EqualTo(7));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Energy"], Is.EqualTo(8));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gems"], Is.EqualTo(9));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems.Count, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].ItemId, Is.EqualTo("Sapphire_Pack"));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void ServerRewardPlayerStateSyncService_SyncFromGlobalSaveAsync_AppliesSnapshot_WhenEnvelopeDataObject()
        {
            var snapshotHandler = new StubSnapshotHandler();
            var webClient = new StubWebClient
            {
                GetResponder = (url, responseType) =>
                {
                    return JObject.Parse(
                        "{ \"success\": true, \"data\": { " +
                        "\"resources\": { \"Gold\": 11, \"Energy\": 12, \"Gems\": 13 }, " +
                        "\"inventory\": { \"inventoryItems\": { \"Ruby_Pack\": { \"stackCount\": 4 } } } } }");
                }
            };
            var service = new ServerRewardPlayerStateSyncService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                new[] { snapshotHandler });

            service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gold"], Is.EqualTo(11));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Energy"], Is.EqualTo(12));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gems"], Is.EqualTo(13));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems.Count, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].ItemId, Is.EqualTo("Ruby_Pack"));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].Amount, Is.EqualTo(4));
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

        private sealed class StubSnapshotHandler : IPlayerStateSnapshotHandler
        {
            public int AppliedCount { get; private set; }
            public PlayerStateSnapshotDto LastSnapshot { get; private set; }

            public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                AppliedCount++;
                LastSnapshot = snapshot;
                return UniTask.CompletedTask;
            }
        }

        private sealed class StubWebClient : IWebClient
        {
            public Func<string, Type, object> GetResponder { get; set; }
            public Exception GetException { get; set; }
            public Func<string, Type, Type, object, object> PostResponder { get; set; }
            public Exception PostException { get; set; }

            public UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (GetException != null)
                {
                    throw GetException;
                }

                if (GetResponder == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                var response = GetResponder(url, typeof(TResponse));
                if (response == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                if (response is TResponse typed)
                {
                    return UniTask.FromResult(typed);
                }

                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                var converted = Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>(serialized);
                return UniTask.FromResult(converted);
            }

            public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (PostException != null)
                {
                    throw PostException;
                }

                if (PostResponder == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                var response = PostResponder(url, typeof(TRequest), typeof(TResponse), data);
                if (response == null)
                {
                    return UniTask.FromResult(default(TResponse));
                }

                if (response is TResponse typed)
                {
                    return UniTask.FromResult(typed);
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
