using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CardCollection.Tests
{
    public class CardCollectionPointsBalanceTests
    {
        private ICardCollectionApplicationFacade _facade;

        [TearDown]
        public void TearDown()
        {
            _facade?.Dispose();
            _facade = null;
        }

        [UnityTest]
        public IEnumerator AddPointsAsync_InternalMethod_AddsPointsToCollectionBalance()
        {
            yield return CreateFacadeInitialized(points: 5).ToCoroutine(result => _facade = result);
            yield return _facade.TryAddPointsAsync(7, CancellationToken.None).ToCoroutine(_ => { });

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);
            Assert.AreEqual(12, points);
        }

        [UnityTest]
        public IEnumerator AddPointsAsync_InternalMethod_CanBeAppliedMultipleTimes()
        {
            yield return CreateFacadeInitialized(points: 2).ToCoroutine(result => _facade = result);
            yield return _facade.TryAddPointsAsync(3, CancellationToken.None).ToCoroutine(_ => { });
            yield return _facade.TryAddPointsAsync(4, CancellationToken.None).ToCoroutine(_ => { });

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);
            Assert.AreEqual(9, points);
        }

        [UnityTest]
        public IEnumerator TrySpendPointsAsync_WhenEnoughPoints_ReturnsTrueAndDecreasesBalance()
        {
            yield return CreateFacadeInitialized(points: 10).ToCoroutine(result => _facade = result);

            bool spendResult = false;
            yield return _facade.TrySpendPointsAsync(6).ToCoroutine(result => spendResult = result);

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);

            Assert.IsTrue(spendResult);
            Assert.AreEqual(4, points);
        }

        [UnityTest]
        public IEnumerator TrySpendPointsAsync_WhenNotEnoughPoints_ReturnsFalseAndKeepsBalance()
        {
            yield return CreateFacadeInitialized(points: 3).ToCoroutine(result => _facade = result);

            bool spendResult = true;
            yield return _facade.TrySpendPointsAsync(5).ToCoroutine(result => spendResult = result);

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);

            Assert.IsFalse(spendResult);
            Assert.AreEqual(3, points);
        }

        [UnityTest]
        public IEnumerator AddAndSpendPoints_WithZeroValues_KeepBalanceUnchanged()
        {
            yield return CreateFacadeInitialized(points: 8).ToCoroutine(result => _facade = result);
            yield return _facade.TryAddPointsAsync(0, CancellationToken.None).ToCoroutine(_ => { });

            bool spendResult = false;
            yield return _facade.TrySpendPointsAsync(0).ToCoroutine(result => spendResult = result);

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);

            Assert.IsTrue(spendResult);
            Assert.AreEqual(8, points);
        }

        [UnityTest]
        public IEnumerator AddAndSpendPoints_WithNegativeValues_KeepBalanceUnchanged()
        {
            yield return CreateFacadeInitialized(points: 8).ToCoroutine(result => _facade = result);
            yield return _facade.TryAddPointsAsync(-5, CancellationToken.None).ToCoroutine(_ => { });

            bool spendResult = false;
            yield return _facade.TrySpendPointsAsync(-3).ToCoroutine(result => spendResult = result);

            int points = 0;
            yield return _facade.GetCollectionPoints().ToCoroutine(result => points = result);

            Assert.IsTrue(spendResult);
            Assert.AreEqual(8, points);
        }

        private static async UniTask<ICardCollectionApplicationFacade> CreateFacadeInitialized(int points)
        {
            var facade = CreateFacade(points);
            await facade.InitializeAsync(CancellationToken.None);
            return facade;
        }

        private static CardCollectionApplicationFacade CreateFacade(int points)
        {
            const string eventId = "test";

            var packProvider = new StubPackProvider("test_pack");
            var storage = new InMemoryEventCardsStorage(CreateSaveData(eventId, points));
            var definitionProvider = new StubCardDefinitionProvider(new List<CardDefinition>());
            var selector = new StubCardSelector();
            var pointsCalculator = new CardCollectionDuplicatePointsTests.MockCardPointsCalculator();
            var cardPackService = new CardPackService(packProvider.Data);
            var cardRandomizer = new PackBasedCardsRandomizer(selector, definitionProvider);
            var cardProgressService = new CardProgressService(storage, definitionProvider, pointsCalculator);
            var openUseCase = new OpenPackUseCase(cardPackService, cardRandomizer, cardProgressService, definitionProvider);
            var unlockUseCase = new UnlockCardsUseCase(cardProgressService, definitionProvider);
            var pointsService = new PointsAccountService(cardProgressService);
            var queryService = new CollectionProgressQueryService(cardProgressService);
            return new CardCollectionApplicationFacade(
                eventId,
                definitionProvider,
                cardPackService,
                cardProgressService,
                openUseCase,
                unlockUseCase,
                pointsService,
                queryService);
        }

        private static EventCardsSaveData CreateSaveData(string eventId, int points)
        {
            return new EventCardsSaveData
            {
                EventId = eventId,
                Version = 1,
                Points = points
            };
        }

        private sealed class StubPackProvider : IStaticDataProvider<List<CardPackConfig>>
        {
            private readonly List<CardPackConfig> _packs;

            public StubPackProvider(string packId)
            {
                _packs = new List<CardPackConfig>
                {
                    new()
                    {
                        packId = packId,
                        packName = "Test Pack",
                        cardCount = 1,
                    }
                };
            }

            public UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_packs);
            }

            public List<CardPackConfig> Data => _packs;

            public UniTask<List<CardPackConfig>> LoadAsync(string fileName, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_packs);
            }

            public void ClearCache()
            {
            }
        }

        private sealed class StubCardDefinitionProvider : ICardDefinitionProvider
        {
            private readonly List<CardDefinition> _cardDefinitions;
            private readonly Dictionary<string, CardDefinition> _cardDefinitionsById;

            public StubCardDefinitionProvider(List<CardDefinition> cardDefinitions)
            {
                _cardDefinitions = cardDefinitions;
                _cardDefinitionsById = cardDefinitions.ToDictionary(card => card.Id, card => card);
            }

            public List<CardDefinition> GetCardDefinitions() => _cardDefinitions;
            public IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById() => _cardDefinitionsById;
        }

        private sealed class StubCardSelector : ICardSelector
        {
            public UniTask<List<string>> SelectCardsAsync(
                CardPack pack,
                List<CardDefinition> allCards,
                string eventId,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new List<string>());
            }
        }

        private sealed class InMemoryEventCardsStorage : IEventCardsStorage
        {
            private EventCardsSaveData _data;

            public InMemoryEventCardsStorage(EventCardsSaveData initialData)
            {
                _data = Clone(initialData);
            }

            public UniTask InitializeAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (_data == null || _data.EventId != eventId)
                {
                    return UniTask.FromResult(new EventCardsSaveData
                    {
                        EventId = eventId,
                        Version = 1,
                        Points = 0
                    });
                }

                return UniTask.FromResult(Clone(_data));
            }

            public UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                _data = Clone(data);
                return UniTask.CompletedTask;
            }

            public async UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (cardIds == null || cardIds.Count == 0)
                {
                    return;
                }

                foreach (var cardId in cardIds)
                {
                    ct.ThrowIfCancellationRequested();
                    var card = data.Cards.Find(c => c.CardId == cardId);
                    if (card == null)
                    {
                        data.Cards.Add(new CardProgressData
                        {
                            CardId = cardId,
                            IsUnlocked = true,
                            IsNew = true
                        });
                        continue;
                    }

                    card.IsUnlocked = true;
                    card.IsNew = true;
                }

                await SaveAsync(data, ct);
            }

            public UniTask ClearCollectionAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                _data = null;
                return UniTask.CompletedTask;
            }

            public UniTask DeleteAsync(string eventId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (_data != null && _data.EventId == eventId)
                {
                    _data = null;
                }

                return UniTask.CompletedTask;
            }

            public void Dispose()
            {
            }

            private static EventCardsSaveData Clone(EventCardsSaveData source)
            {
                if (source == null)
                {
                    return null;
                }

                return new EventCardsSaveData
                {
                    EventId = source.EventId,
                    Version = source.Version,
                    Points = source.Points,
                    Cards = source.Cards.Select(card => new CardProgressData
                    {
                        CardId = card.CardId,
                        IsUnlocked = card.IsUnlocked,
                        IsNew = card.IsNew
                    }).ToList()
                };
            }
        }
    }
}
