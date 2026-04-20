using System;
using System.Collections.Generic;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;

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

            public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                AppliedCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class StubWebClient : IWebClient
        {
            public Func<string, Type, Type, object, object> PostResponder { get; set; }
            public Exception PostException { get; set; }

            public UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                throw new NotSupportedException();
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
