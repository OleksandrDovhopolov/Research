using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using NUnit.Framework;

namespace EventOrchestration.Tests.Editor
{
    public sealed class ServerLiveOpsScheduleProviderTests
    {
        [Test]
        public void LoadAsync_WhenServerReturnsSchedule_ReturnsItemsWithoutReordering()
        {
            var expected = new List<ScheduleItem>
            {
                new()
                {
                    Id = "card_collection",
                    EventType = "CardCollection",
                    StartTimeUtc = DateTimeOffset.Parse("2026-04-16T08:39:03Z"),
                    EndTimeUtc = DateTimeOffset.Parse("2026-04-27T22:25:43Z"),
                    Priority = 10,
                    StreamId = "card_collection_seasons",
                    CustomParams = new Dictionary<string, string>
                    {
                        ["eventId"] = "Winter_Collection",
                        ["collectionName"] = "Winter Collection",
                        ["eventConfigAddress"] = "event_winter_collection_config",
                    },
                },
                new()
                {
                    Id = "battle_pass",
                    EventType = "BattlePass",
                    StartTimeUtc = DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    EndTimeUtc = DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    Priority = 5,
                    StreamId = "battle_pass",
                    CustomParams = new Dictionary<string, string>(),
                },
            };

            var webClient = new FakeWebClient(_ => UniTask.FromResult<object>(expected));
            var provider = new ServerLiveOpsScheduleProvider(webClient);

            var actual = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(webClient.LastRequestedUrl, Is.EqualTo("liveops/schedule"));
            Assert.That(actual.Count, Is.EqualTo(2));
            Assert.That(actual[0].Id, Is.EqualTo("card_collection"));
            Assert.That(actual[1].EventType, Is.EqualTo("BattlePass"));
            Assert.That(actual[0].CustomParams["eventConfigAddress"], Is.EqualTo("event_winter_collection_config"));
        }

        [Test]
        public void LoadAsync_WhenCustomParamsIsNull_NormalizesToEmptyDictionary()
        {
            var expected = new List<ScheduleItem>
            {
                new()
                {
                    Id = "battle_pass",
                    EventType = "BattlePass",
                    StartTimeUtc = DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    EndTimeUtc = DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    Priority = 5,
                    StreamId = "battle_pass",
                    CustomParams = null,
                },
            };

            var provider = new ServerLiveOpsScheduleProvider(new FakeWebClient(_ => UniTask.FromResult<object>(expected)));

            var actual = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(actual.Count, Is.EqualTo(1));
            Assert.That(actual[0].CustomParams, Is.Not.Null);
            Assert.That(actual[0].CustomParams.Count, Is.EqualTo(0));
        }

        [Test]
        public void LoadAsync_WhenServerFailsAfterSuccessfulLoad_ReturnsLastValidSnapshot()
        {
            var firstResponse = new List<ScheduleItem>
            {
                new()
                {
                    Id = "card_collection",
                    EventType = "CardCollection",
                    StartTimeUtc = DateTimeOffset.Parse("2026-04-16T08:39:03Z"),
                    EndTimeUtc = DateTimeOffset.Parse("2026-04-27T22:25:43Z"),
                    Priority = 10,
                    StreamId = "card_collection_seasons",
                    CustomParams = new Dictionary<string, string>
                    {
                        ["eventId"] = "Winter_Collection",
                    },
                },
            };

            var webClient = new SequencedWebClient(
                _ => UniTask.FromResult<object>(firstResponse),
                _ => throw new WebClientHttpException("https://example/liveops/schedule", 500, "Internal error while loading liveops schedule."));
            var provider = new ServerLiveOpsScheduleProvider(webClient);

            var initialSnapshot = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var fallbackSnapshot = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(initialSnapshot.Count, Is.EqualTo(1));
            Assert.That(fallbackSnapshot.Count, Is.EqualTo(1));
            Assert.That(fallbackSnapshot[0].Id, Is.EqualTo("card_collection"));
            Assert.That(fallbackSnapshot[0].CustomParams["eventId"], Is.EqualTo("Winter_Collection"));
        }

        [Test]
        public void LoadAsync_WhenFirstRequestFails_ReturnsEmptySnapshot()
        {
            var provider = new ServerLiveOpsScheduleProvider(
                new FakeWebClient(_ => throw new WebClientNetworkException("https://example/liveops/schedule", "offline")));

            var actual = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(actual, Is.Empty);
        }

        private sealed class FakeWebClient : IWebClient
        {
            private readonly Func<string, UniTask<object>> _getHandler;

            public FakeWebClient(Func<string, UniTask<object>> getHandler)
            {
                _getHandler = getHandler;
            }

            public string LastRequestedUrl { get; private set; }

            public async UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                LastRequestedUrl = url;
                var response = await _getHandler(url);
                return (TResponse)response;
            }

            public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class SequencedWebClient : IWebClient
        {
            private readonly Queue<Func<string, UniTask<object>>> _responses;

            public SequencedWebClient(params Func<string, UniTask<object>>[] responses)
            {
                _responses = new Queue<Func<string, UniTask<object>>>(responses);
            }

            public async UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException("No more queued responses.");
                }

                var response = await _responses.Dequeue()(url);
                return (TResponse)response;
            }

            public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
