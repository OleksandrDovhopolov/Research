using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public sealed class ServerCardSelectorWebClientTests
    {
        [Test]
        public void SelectCardsAsync_ReturnsOpenedCardIds_FromWebClientResponse()
        {
            var webClient = new StubWebClient();
            var selector = new ServerCardSelector(webClient);
            var pack = new CardPack(new CardPackConfig
            {
                packId = "pack_1",
                cardCount = 3,
                packName = "Pack 1"
            });

            var result = selector.SelectCardsAsync(pack, new List<CardDefinition>(), "event_1", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0], Is.EqualTo("card_a"));
            Assert.That(result[1], Is.EqualTo("card_b"));
        }

        private sealed class StubWebClient : IWebClient
        {
            public UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
            {
                throw new System.NotSupportedException();
            }

            public UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                var response = new OpenPackResponse
                {
                    OpenedCardIds = new List<string> { "card_a", "card_b" }
                };
                return UniTask.FromResult((TResponse)(object)response);
            }

            public UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
