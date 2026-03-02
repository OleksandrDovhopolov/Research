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
        private CardCollectionModule _module;

        [TearDown]
        public void TearDown()
        {
            _module?.Dispose();
            _module = null;
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenLastMissingCardIsUnlocked_RaisesCollectionCompletedWithEventId()
        {
            const string eventId = "event-test";

            _module = CreateModule(
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
                    ("b2", false)));

            int notificationsCount = 0;
            string receivedEventId = null;
            _module.OnCollectionCompleted += data =>
            {
                notificationsCount++;
                receivedEventId = data.EventId;
            };

            yield return _module.InitializeAsync().ToCoroutine();
            yield return _module.UnlockCard("b2").ToCoroutine();

            Assert.AreEqual(1, notificationsCount);
            Assert.AreEqual(eventId, receivedEventId);
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenCollectionAlreadyCompleted_DoesNotRaiseCollectionCompletedAgain()
        {
            const string eventId = "event-test";

            _module = CreateModule(
                eventId,
                CreateDefinitions(
                    ("a1", "group-a"),
                    ("a2", "group-a")),
                CreateSaveData(
                    eventId,
                    ("a1", true),
                    ("a2", true)));

            int notificationsCount = 0;
            _module.OnCollectionCompleted += _ => notificationsCount++;

            yield return _module.InitializeAsync().ToCoroutine();
            yield return _module.UnlockCard("a1").ToCoroutine();
            yield return _module.UnlockCard("a2").ToCoroutine();

            Assert.AreEqual(0, notificationsCount);
        }

        [UnityTest]
        public IEnumerator UnlockCard_WhenCollectionBecomesCompleted_RaisesCollectionCompletedOnlyOnce()
        {
            const string eventId = "event-test";

            _module = CreateModule(
                eventId,
                CreateDefinitions(
                    ("a1", "group-a"),
                    ("a2", "group-a")),
                CreateSaveData(
                    eventId,
                    ("a1", true),
                    ("a2", false)));

            int notificationsCount = 0;
            _module.OnCollectionCompleted += _ => notificationsCount++;

            yield return _module.InitializeAsync().ToCoroutine();
            yield return _module.UnlockCard("a2").ToCoroutine();
            yield return _module.UnlockCard("a2").ToCoroutine();

            Assert.AreEqual(1, notificationsCount);
        }

        private static CardCollectionModule CreateModule(
            string eventId,
            List<CardDefinition> definitions,
            EventCardsSaveData initialData)
        {
            var config = new CardCollectionModuleConfig(
                new StubPackProvider(),
                new InMemoryEventCardsStorage(initialData),
                new StubCardDefinitionProvider(definitions),
                new StubCardSelector(),
                new DefaultCardPointsCalculator(),
                eventId);

            return new CardCollectionModule(config);
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

        private sealed class StubPackProvider : ICardPackProvider
        {
            private static readonly List<CardPackConfig> EmptyPacks = new();

            public UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(EmptyPacks);
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
                CardSelectionContext context,
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

            public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                if (cardIds == null || cardIds.Count == 0)
                {
                    return;
                }

                var data = await LoadAsync(eventId, ct);
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
