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
    public class CardCollectionDuplicatePointsTests
    {
        private ICardCollectionApplicationFacade _facade;

        [TearDown]
        public void TearDown()
        {
            _facade?.Dispose();
            _facade = null;
        }

        [UnityTest]
        public IEnumerator OpenPackAndUnlockAsync_WhenAllOpenedCardsAreDuplicates_AddsExpectedPointsByCardType()
        {
            const string eventId = "test";
            const string packId = "test_pack";

            var cardDefinitions = CreateCardDefinitions(
                ("1", 1, false),
                ("2", 2, false),
                ("3", 3, false),
                ("4", 4, false),
                ("5", 5, false),
                ("6", 5, true));

            var openedCardIds = new List<string> { "1", "2", "3", "4", "5", "6" };
            var initialData = CreateSaveData(
                eventId,
                points: 0,
                ("1", true),
                ("2", true),
                ("3", true),
                ("4", true),
                ("5", true),
                ("6", true));

            yield return CreateFacadeInitialized(eventId, packId, openedCardIds.Count, cardDefinitions, openedCardIds, initialData)
                .ToCoroutine(result => _facade = result);
            yield return _facade.OpenPackAndUnlockAsync(packId).ToCoroutine(_ => { });

            EventCardsSaveData updatedData = null;
            yield return _facade.Load().ToCoroutine(result => updatedData = result);

            Assert.NotNull(updatedData);
            Assert.AreEqual(31, updatedData.Points, "Expected duplicate points: 1+2+3+5+10+10 = 31");
        }

        [UnityTest]
        public IEnumerator OpenPackAndUnlockAsync_WhenPackContainsOwnedAndNewCards_AddsPointsOnlyForOwnedCards()
        {
            const string eventId = "test";
            const string packId = "example_pack";

            var cardDefinitions = CreateCardDefinitions(
                ("5", 1, false),
                ("10", 2, false),
                ("150", 5, true));

            var openedCardIds = new List<string> { "5", "10", "150" };
            var initialData = CreateSaveData(
                eventId,
                points: 7,
                ("5", true),
                ("10", true),
                ("150", false));

            yield return CreateFacadeInitialized(eventId, packId, openedCardIds.Count, cardDefinitions, openedCardIds, initialData)
                .ToCoroutine(result => _facade = result);
            yield return _facade.OpenPackAndUnlockAsync(packId).ToCoroutine(_ => { });

            EventCardsSaveData updatedData = null;
            yield return _facade.Load().ToCoroutine(result => updatedData = result);

            Assert.NotNull(updatedData);
            Assert.AreEqual(10, updatedData.Points, "Expected only card 5 and 10 to add points: 7 + 1 + 2 = 10");

            var newlyOpenedCard = updatedData.Cards.Find(card => card.CardId == "150");
            Assert.NotNull(newlyOpenedCard);
            Assert.IsTrue(newlyOpenedCard.IsUnlocked);
        }

        private static async UniTask<ICardCollectionApplicationFacade> CreateFacadeInitialized(
            string eventId,
            string packId,
            int packCardCount,
            List<CardDefinition> cardDefinitions,
            List<string> openedCardIds,
            EventCardsSaveData initialData)
        {
            var facade = CreateFacade(eventId, packId, packCardCount, cardDefinitions, openedCardIds, initialData);
            await facade.InitializeAsync(CancellationToken.None);
            return facade;
        }

        private static CardCollectionApplicationFacade CreateFacade(
            string eventId,
            string packId,
            int packCardCount,
            List<CardDefinition> cardDefinitions,
            List<string> openedCardIds,
            EventCardsSaveData initialData)
        {
            var packProvider = new StubPackProvider(packId, packCardCount);
            var storage = new InMemoryEventCardsStorage(initialData);
            var definitionProvider = new StubCardDefinitionProvider(cardDefinitions);
            var selector = new StubCardSelector(openedCardIds);
            var pointsCalculator = new MockCardPointsCalculator();
            var cardPackService = new CardPackService(packProvider);
            var cardRandomizer = new PackBasedCardsRandomizer(selector, definitionProvider);
            var cardProgressService = new CardProgressService(storage);
            var duplicateCalculator = new DuplicateCardPointsCalculator(definitionProvider, pointsCalculator);

            var openUseCase = new OpenPackUseCase(cardPackService, cardRandomizer, cardProgressService, duplicateCalculator, definitionProvider);
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

        private static EventCardsSaveData CreateSaveData(
            string eventId,
            int points,
            params (string cardId, bool isUnlocked)[] cards)
        {
            var data = new EventCardsSaveData
            {
                EventId = eventId,
                Version = 1,
                Points = points
            };

            foreach (var (cardId, isUnlocked) in cards)
            {
                data.Cards.Add(new CardProgressData
                {
                    CardId = cardId,
                    IsUnlocked = isUnlocked,
                    IsNew = false
                });
            }

            return data;
        }

        private static List<CardDefinition> CreateCardDefinitions(params (string id, int stars, bool premiumCard)[] cards)
        {
            return cards.Select(card => new CardDefinition
            {
                Id = card.id,
                CardName = card.id,
                GroupType = "test",
                Stars = card.stars,
                PremiumCard = card.premiumCard,
                Icon = string.Empty
            }).ToList();
        }

        private sealed class StubPackProvider : ICardPackProvider
        {
            private readonly List<CardPackConfig> _packs;

            public StubPackProvider(string packId, int cardCount)
            {
                _packs = new List<CardPackConfig>
                {
                    new()
                    {
                        packId = packId,
                        packName = "Test Pack",
                        cardCount = cardCount,
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

        private sealed class StubCardSelector : ICardSelector
        {
            private readonly List<string> _openedCardIds;

            public StubCardSelector(List<string> openedCardIds)
            {
                _openedCardIds = openedCardIds;
            }

            public async UniTask<List<string>> SelectCardsAsync(
                CardPack pack,
                List<CardDefinition> allCards,
                CancellationToken ct = default)
            {
                await UniTask.Yield(ct);
                ct.ThrowIfCancellationRequested();
                return _openedCardIds.Take(pack.CardCount).ToList();
            }
        }

        private sealed class StubCardDefinitionProvider : ICardDefinitionProvider
        {
            private readonly List<CardDefinition> _cardDefinitions;
            private readonly Dictionary<string, CardDefinition> _cardDefinitionsById;

            public StubCardDefinitionProvider(List<CardDefinition> cardDefinitions)
            {
                _cardDefinitions = cardDefinitions;
                _cardDefinitionsById = cardDefinitions
                    .Where(card => !string.IsNullOrEmpty(card.Id))
                    .ToDictionary(card => card.Id, card => card);
            }

            public List<CardDefinition> GetCardDefinitions() => _cardDefinitions;
            public IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById() => _cardDefinitionsById;
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
        
        public sealed class MockCardPointsCalculator : ICardPointsCalculator
        {
            public int GetPoints(int stars, bool isPremium)
            {
                if (isPremium)
                    return 10;

                return stars switch
                {
                    1 => 1,
                    2 => 2,
                    3 => 3,
                    4 => 5,
                    5 => 10,
                    _ => 0
                };
            }
        }
    }
}
