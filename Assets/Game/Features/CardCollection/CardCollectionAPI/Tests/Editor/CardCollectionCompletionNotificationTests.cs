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
    public class CardCollectionCompletionNotificationTests
    {
        private ICardCollectionApplicationFacade _facade;

        [TearDown]
        public void TearDown()
        {
            _facade?.Dispose();
            _facade = null;
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenLastMissingCardIsUnlocked_RaisesCollectionCompletedWithEventId()
        {
            const string eventId = "event-test";

            yield return CreateFacadeInitialized(
                eventId,
                CreateDefinitions(
                    ("a1", "group-a"),
                    ("a2", "group-a"),
                    ("b1", "group-b"),
                    ("b2", "group-b")),
                CreateSaveData(
                    eventId,
                    ("a1", true),
                    ("a2", true),
                    ("b1", true),
                    ("b2", false))).ToCoroutine(result => _facade = result);

            int notificationsCount = 0;
            string receivedEventId = null;
            _facade.OnCollectionCompleted += data =>
            {
                notificationsCount++;
                receivedEventId = data.EventId;
            };

            yield return _facade.UnlockCards(new[] { "b2" }).ToCoroutine();

            Assert.AreEqual(1, notificationsCount);
            Assert.AreEqual(eventId, receivedEventId);
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenCollectionAlreadyCompleted_DoesNotRaiseCollectionCompletedAgain()
        {
            const string eventId = "event-test";

            yield return CreateFacadeInitialized(
                eventId,
                CreateDefinitions(
                    ("a1", "group-a"),
                    ("a2", "group-a")),
                CreateSaveData(
                    eventId,
                    ("a1", true),
                    ("a2", true))).ToCoroutine(result => _facade = result);

            int notificationsCount = 0;
            _facade.OnCollectionCompleted += _ => notificationsCount++;

            yield return _facade.UnlockCards(new[] { "a1" }).ToCoroutine();
            yield return _facade.UnlockCards(new[] { "a2" }).ToCoroutine();

            Assert.AreEqual(0, notificationsCount);
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenCollectionBecomesCompleted_RaisesCollectionCompletedOnlyOnce()
        {
            const string eventId = "event-test";

            yield return CreateFacadeInitialized(
                eventId,
                CreateDefinitions(
                    ("a1", "group-a"),
                    ("a2", "group-a")),
                CreateSaveData(
                    eventId,
                    ("a1", true),
                    ("a2", false))).ToCoroutine(result => _facade = result);

            int notificationsCount = 0;
            _facade.OnCollectionCompleted += _ => notificationsCount++;

            yield return _facade.UnlockCards(new[] { "a2" }).ToCoroutine();
            yield return _facade.UnlockCards(new[] { "a2" }).ToCoroutine();

            Assert.AreEqual(1, notificationsCount);
        }

        private static async UniTask<ICardCollectionApplicationFacade> CreateFacadeInitialized(
            string eventId,
            List<CardDefinition> definitions,
            EventCardsSaveData initialData)
        {
            var facade = CreateFacade(eventId, definitions, initialData);
            await facade.InitializeAsync(CancellationToken.None);
            return facade;
        }

        private static CardCollectionApplicationFacade CreateFacade(
            string eventId,
            List<CardDefinition> definitions,
            EventCardsSaveData initialData)
        {
            var packProvider = new StubPackProvider();
            var storage = new InMemoryEventCardsStorage(initialData);
            var definitionProvider = new StubCardDefinitionProvider(definitions);
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

        private static List<CardDefinition> CreateDefinitions(params (string id, string groupId)[] cards)
        {
            return cards.Select(card => new CardDefinition
            {
                Id = card.id,
                GroupType = card.groupId,
                CardName = card.id,
                Icon = string.Empty
            }).ToList();
        }

        private static EventCardsSaveData CreateSaveData(string eventId, params (string cardId, bool isUnlocked)[] cards)
        {
            var data = new EventCardsSaveData
            {
                EventId = eventId,
                Version = 1
            };

            foreach (var card in cards)
            {
                data.Cards.Add(new CardProgressData
                {
                    CardId = card.cardId,
                    IsUnlocked = card.isUnlocked
                });
            }

            return data;
        }

        private sealed class StubPackProvider : IStaticDataProvider<List<CardPackConfig>>
        {
            private static readonly List<CardPackConfig> EmptyPacks = new();

            public UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(EmptyPacks);
            }

            public List<CardPackConfig> Data => EmptyPacks;

            public UniTask<List<CardPackConfig>> LoadAsync(string fileName, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(EmptyPacks);
            }

            public void ClearCache()
            {
            }
        }

        private sealed class StubCardDefinitionProvider : ICardDefinitionProvider
        {
            private readonly List<CardDefinition> _definitions;
            private readonly IReadOnlyDictionary<string, CardDefinition> _definitionsById;

            public StubCardDefinitionProvider(List<CardDefinition> definitions)
            {
                _definitions = definitions;
                _definitionsById = definitions.ToDictionary(definition => definition.Id, definition => definition);
            }

            public List<CardDefinition> GetCardDefinitions() => _definitions;

            public IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById() => _definitionsById;
        }

        private sealed class StubCardSelector : ICardSelector
        {
            public UniTask<List<string>> SelectCardsAsync(
                CardPack pack,
                List<CardDefinition> allCards,
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
                        Version = 1
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

                    var card = data.Cards.Find(value => value.CardId == cardId);
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
