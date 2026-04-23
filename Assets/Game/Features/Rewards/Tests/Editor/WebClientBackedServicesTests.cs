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
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

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
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

            var result = service.TryGrantAsync("reward_a", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.False);
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void ServerRewardGrantService_TryGrantDetailedAsync_ReturnsSuccess_WhenResponseSuccessful()
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
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

            var result = service.TryGrantDetailedAsync("Gems", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.True);
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
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

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
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

            var result = service.TryGrantDetailedAsync("Gems", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureType, Is.EqualTo(RewardGrantFailureType.Network));
            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(0));
        }

        [Test]
        public void GrantRewardResponseApplier_TryApplyAsync_ReturnsFalse_WhenResponseIsNull()
        {
            var snapshotApplier = new StubSnapshotApplier();
            var applier = new RewardResponseApplier(snapshotApplier);

            var result = applier.TryApplyAsync(null, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.False);
            Assert.That(snapshotApplier.ApplyCalls, Is.EqualTo(0));
        }

        [Test]
        public void GrantRewardResponseApplier_TryApplyAsync_ReturnsFalse_WhenGrantRejected()
        {
            var snapshotApplier = new StubSnapshotApplier();
            var applier = new RewardResponseApplier(snapshotApplier);
            var response = new GrantRewardResponse
            {
                Success = false,
                RewardId = "reward_a",
                ErrorCode = "REJECTED"
            };

            var result = applier.TryApplyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.False);
            Assert.That(snapshotApplier.ApplyCalls, Is.EqualTo(0));
        }

        [Test]
        public void GrantRewardResponseApplier_TryApplyAsync_ReturnsFalse_WhenPlayerStateMissing()
        {
            var snapshotApplier = new StubSnapshotApplier();
            var applier = new RewardResponseApplier(snapshotApplier);
            var response = new GrantRewardResponse
            {
                Success = true,
                RewardId = "reward_a",
                PlayerState = null
            };

            var result = applier.TryApplyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.False);
            Assert.That(snapshotApplier.ApplyCalls, Is.EqualTo(0));
        }

        [Test]
        public void GrantRewardResponseApplier_TryApplyAsync_CallsSnapshotApplierOnce_WhenResponseValid()
        {
            var snapshotApplier = new StubSnapshotApplier();
            var applier = new RewardResponseApplier(snapshotApplier);
            var response = new GrantRewardResponse
            {
                Success = true,
                RewardId = "reward_a",
                PlayerState = new PlayerStateSnapshotDto
                {
                    Resources = new Dictionary<string, int> { { "gold", 10 } }
                }
            };

            var result = applier.TryApplyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result, Is.True);
            Assert.That(snapshotApplier.ApplyCalls, Is.EqualTo(1));
            Assert.That(snapshotApplier.LastSnapshot, Is.Not.Null);
        }

        [Test]
        public void PlayerStateSnapshotApplier_ApplyAsync_AppliesHandlersInOrder_WhenSnapshotValid()
        {
            var callOrder = new List<string>();
            var first = new OrderedSnapshotHandler("first", callOrder);
            var second = new OrderedSnapshotHandler("second", callOrder);
            var applier = new PlayerStateSnapshotApplier(new IPlayerStateSnapshotHandler[] { first, second });

            applier.ApplyAsync(new PlayerStateSnapshotDto(), CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(callOrder.Count, Is.EqualTo(2));
            Assert.That(callOrder[0], Is.EqualTo("first"));
            Assert.That(callOrder[1], Is.EqualTo("second"));
        }

        [Test]
        public void PlayerStateSnapshotApplier_ApplyAsync_DoesNothing_WhenSnapshotNull()
        {
            var callOrder = new List<string>();
            var first = new OrderedSnapshotHandler("first", callOrder);
            var applier = new PlayerStateSnapshotApplier(new IPlayerStateSnapshotHandler[] { first });

            applier.ApplyAsync(null, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(callOrder.Count, Is.EqualTo(0));
        }

        [Test]
        public void PlayerStateSnapshotApplier_ApplyAsync_ThrowsWhenCanceled()
        {
            var callOrder = new List<string>();
            var first = new OrderedSnapshotHandler("first", callOrder);
            var applier = new PlayerStateSnapshotApplier(new IPlayerStateSnapshotHandler[] { first });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                applier.ApplyAsync(new PlayerStateSnapshotDto(), cts.Token).GetAwaiter().GetResult());
            Assert.That(callOrder.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void ServerRewardGrantService_TryGrantAsync_SerializesConcurrentCalls_AndAppliesLatestSnapshotLast()
        {
            var snapshotHandler = new SequencedSnapshotHandler();
            var webClient = new ControlledGrantWebClient();
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

            var firstGrantTask = service.TryGrantAsync("reward_first", CancellationToken.None);
            webClient.WaitForFirstCallAsync(CancellationToken.None).GetAwaiter().GetResult();

            var secondGrantTask = service.TryGrantAsync("reward_second", CancellationToken.None);

            // With grant serialization, second call must not reach web client before first is released.
            Assert.That(webClient.HasSecondCallStarted(100), Is.False);

            webClient.ReleaseFirstCall();

            var firstResult = firstGrantTask.GetAwaiter().GetResult();
            var secondResult = secondGrantTask.GetAwaiter().GetResult();

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.True);
            Assert.That(webClient.PostCallsCount, Is.EqualTo(2));
            Assert.That(snapshotHandler.AppliedGoldHistory.Count, Is.EqualTo(2));
            Assert.That(snapshotHandler.AppliedGoldHistory[0], Is.EqualTo(100));
            Assert.That(snapshotHandler.AppliedGoldHistory[1], Is.EqualTo(200));
            Assert.That(snapshotHandler.LastGold, Is.EqualTo(200));
        }
        
        [Test]
        public void ServerRewardGrantService_TryGrantAsync_WhenCanceledWhileQueued_DoesNotReachWebClient()
        {
            var snapshotHandler = new SequencedSnapshotHandler();
            var webClient = new ControlledGrantWebClient();
            var service = new ServerRewardGrantService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateRewardResponseApplier(snapshotHandler));

            var firstGrantTask = service.TryGrantAsync("reward_first", CancellationToken.None);
            webClient.WaitForFirstCallAsync(CancellationToken.None).GetAwaiter().GetResult();

            using var secondCts = new CancellationTokenSource();
            var secondGrantTask = service.TryGrantAsync("reward_second", secondCts.Token);
            secondCts.Cancel();

            Assert.Throws<OperationCanceledException>(() => secondGrantTask.GetAwaiter().GetResult());

            webClient.ReleaseFirstCall();
            Assert.That(firstGrantTask.GetAwaiter().GetResult(), Is.True);
            Assert.That(webClient.PostCallsCount, Is.EqualTo(1));
            Assert.That(snapshotHandler.AppliedGoldHistory.Count, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastGold, Is.EqualTo(100));
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
                CreateSnapshotApplier(snapshotHandler));

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
                CreateSnapshotApplier(snapshotHandler));

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
                CreateSnapshotApplier(snapshotHandler));

            service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotHandler.AppliedCount, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gold"], Is.EqualTo(11));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Energy"], Is.EqualTo(12));
            Assert.That(snapshotHandler.LastSnapshot.Resources["Gems"], Is.EqualTo(13));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems.Count, Is.EqualTo(1));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].ItemId, Is.EqualTo("Ruby_Pack"));
            Assert.That(snapshotHandler.LastSnapshot.InventoryItems[0].Amount, Is.EqualTo(4));
        }

        [Test]
        public void ServerRewardPlayerStateSyncService_SyncFromGlobalSaveAsync_Throws_WhenEnvelopeDataIsEmpty()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (_, __) => JObject.Parse("{ \"success\": true, \"data\": null }")
            };
            var service = new ServerRewardPlayerStateSyncService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateSnapshotApplier(new StubSnapshotHandler()));

            Assert.Throws<InvalidOperationException>(() =>
                service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void ServerRewardPlayerStateSyncService_SyncFromGlobalSaveAsync_Throws_WhenEnvelopeDataStringInvalidJson()
        {
            var webClient = new StubWebClient
            {
                GetResponder = (_, __) => JObject.Parse("{ \"success\": true, \"data\": \"not json\" }")
            };
            var service = new ServerRewardPlayerStateSyncService(
                new StubPlayerIdentityProvider("player-1"),
                webClient,
                CreateSnapshotApplier(new StubSnapshotHandler()));

            Assert.Throws<InvalidOperationException>(() =>
                service.SyncFromGlobalSaveAsync(CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void SaveGlobalPayloadParser_ExtractPayloadStrict_Throws_WhenDataFieldIsEmpty()
        {
            var token = JObject.Parse("{ \"success\": true, \"data\": null }");

            Assert.Throws<InvalidOperationException>(() => SaveGlobalPayloadParser.ExtractPayloadStrict(token));
        }

        [Test]
        public void SaveGlobalPayloadParser_ExtractPayloadStrict_Throws_WhenDataStringInvalidJson()
        {
            var token = JObject.Parse("{ \"success\": true, \"data\": \"invalid\" }");

            Assert.Throws<InvalidOperationException>(() => SaveGlobalPayloadParser.ExtractPayloadStrict(token));
        }

        [Test]
        public void SaveGlobalPayloadParser_ExtractDataForStorage_ReturnsDataStringAndMode()
        {
            const string response = "{ \"success\": true, \"data\": \"{\\\"foo\\\":1}\" }";

            var normalized = SaveGlobalPayloadParser.ExtractDataForStorage(response, out var mode);

            Assert.That(mode, Is.EqualTo("data-string"));
            Assert.That(normalized, Is.EqualTo("{\"foo\":1}"));
        }

        [Test]
        public void SaveGlobalPayloadParser_ExtractDataForStorage_ReturnsRawTextMode_WhenResponseNotJson()
        {
            const string response = "not json";

            var normalized = SaveGlobalPayloadParser.ExtractDataForStorage(response, out var mode);

            Assert.That(mode, Is.EqualTo("raw-text"));
            Assert.That(normalized, Is.EqualTo(response));
        }

        private static IRewardResponseApplier CreateRewardResponseApplier(params IPlayerStateSnapshotHandler[] snapshotHandlers)
        {
            return new RewardResponseApplier(CreateSnapshotApplier(snapshotHandlers));
        }

        private static IPlayerStateSnapshotApplier CreateSnapshotApplier(params IPlayerStateSnapshotHandler[] snapshotHandlers)
        {
            return new PlayerStateSnapshotApplier(snapshotHandlers);
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

        private sealed class StubSnapshotApplier : IPlayerStateSnapshotApplier
        {
            public int ApplyCalls { get; private set; }
            public PlayerStateSnapshotDto LastSnapshot { get; private set; }

            public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                ApplyCalls++;
                LastSnapshot = snapshot;
                return UniTask.CompletedTask;
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
        
        private sealed class SequencedSnapshotHandler : IPlayerStateSnapshotHandler
        {
            private readonly object _sync = new();

            public List<int> AppliedGoldHistory { get; } = new();
            public int LastGold { get; private set; }

            public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                var gold = 0;
                if (snapshot?.Resources != null)
                {
                    if (!snapshot.Resources.TryGetValue("Gold", out gold) &&
                        !snapshot.Resources.TryGetValue("gold", out gold))
                    {
                        gold = 0;
                    }
                }

                lock (_sync)
                {
                    LastGold = gold;
                    AppliedGoldHistory.Add(gold);
                }

                return UniTask.CompletedTask;
            }
        }

        private sealed class OrderedSnapshotHandler : IPlayerStateSnapshotHandler
        {
            private readonly string _id;
            private readonly List<string> _callOrder;

            public OrderedSnapshotHandler(string id, List<string> callOrder)
            {
                _id = id;
                _callOrder = callOrder;
            }

            public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                _callOrder.Add(_id);
                return UniTask.CompletedTask;
            }
        }
        
        private sealed class ControlledGrantWebClient : IWebClient
        {
            private readonly UniTaskCompletionSource _firstCallStarted = new();
            private readonly UniTaskCompletionSource _releaseFirstCall = new();
            private readonly ManualResetEventSlim _secondCallStarted = new(false);
            private int _postCallsCount;

            public int PostCallsCount => _postCallsCount;

            public UniTask WaitForFirstCallAsync(CancellationToken ct)
            {
                return _firstCallStarted.Task.AttachExternalCancellation(ct);
            }

            public bool HasSecondCallStarted(int timeoutMs)
            {
                return _secondCallStarted.Wait(timeoutMs);
            }

            public void ReleaseFirstCall()
            {
                _releaseFirstCall.TrySetResult();
            }

            public UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(default(TResponse));
            }

            public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                var callIndex = Interlocked.Increment(ref _postCallsCount);

                GrantRewardResponse response;
                if (callIndex == 1)
                {
                    _firstCallStarted.TrySetResult();
                    await _releaseFirstCall.Task.AttachExternalCancellation(ct);
                    response = BuildGrantResponse("reward_first", 100);
                }
                else
                {
                    _secondCallStarted.Set();
                    response = BuildGrantResponse("reward_second", 200);
                }

                object boxed = response;
                if (boxed is TResponse typed)
                {
                    return typed;
                }

                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(boxed);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TResponse>(serialized);
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            private static GrantRewardResponse BuildGrantResponse(string rewardId, int gold)
            {
                return new GrantRewardResponse
                {
                    Success = true,
                    RewardId = rewardId,
                    PlayerState = new PlayerStateSnapshotDto
                    {
                        Resources = new Dictionary<string, int>
                        {
                            { "Gold", gold },
                            { "Energy", 0 },
                            { "Gems", 0 }
                        }
                    }
                };
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
