using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using NUnit.Framework;

namespace CardCollection.Tests
{
    public class CardProgressServiceDuplicatePointsTests
    {
        [Test]
        public void AddDuplicatePointsAsync_WhenAllOpenedCardsAreDuplicates_AddsExpectedPoints()
        {
            var eventId = "test";
            var service = CreateService(
                CreateSaveData(
                    eventId,
                    points: 0,
                    ("1", true),
                    ("2", true),
                    ("3", true),
                    ("4", true),
                    ("5", true),
                    ("6", true)),
                CreateCardDefinitions(
                    ("1", 1, false),
                    ("2", 2, false),
                    ("3", 3, false),
                    ("4", 4, false),
                    ("5", 5, false),
                    ("6", 5, true)),
                new DefaultPointsCalculator());

            var awardedPoints = service
                .AddDuplicatePointsAsync(eventId, new[] { "1", "2", "3", "4", "5", "6" })
                .GetAwaiter()
                .GetResult();

            var data = service.LoadAsync(eventId).GetAwaiter().GetResult();

            Assert.AreEqual(31, awardedPoints);
            Assert.AreEqual(31, data.Points);
        }

        [Test]
        public void AddDuplicatePointsAsync_WhenPackContainsOwnedAndNewCards_CountsOnlyOwnedDuplicates()
        {
            var eventId = "test";
            var service = CreateService(
                CreateSaveData(
                    eventId,
                    points: 7,
                    ("5", true),
                    ("10", true),
                    ("150", false)),
                CreateCardDefinitions(
                    ("5", 1, false),
                    ("10", 2, false),
                    ("150", 5, true)),
                new DefaultPointsCalculator());

            var awardedPoints = service
                .AddDuplicatePointsAsync(eventId, new[] { "5", "10", "150" })
                .GetAwaiter()
                .GetResult();

            var data = service.LoadAsync(eventId).GetAwaiter().GetResult();

            Assert.AreEqual(3, awardedPoints);
            Assert.AreEqual(10, data.Points);
        }

        [Test]
        public void AddDuplicatePointsAsync_WhenIdsAreUnknownOrPointsNonPositive_IgnoresThoseCards()
        {
            var eventId = "test";
            var service = CreateService(
                CreateSaveData(
                    eventId,
                    points: 5,
                    ("1", true),
                    ("2", true)),
                CreateCardDefinitions(
                    ("1", 1, false),
                    ("2", 2, false)),
                new NonPositivePointsCalculator());

            var awardedPoints = service
                .AddDuplicatePointsAsync(eventId, new[] { "missing", "", null, "1", "2" })
                .GetAwaiter()
                .GetResult();

            var data = service.LoadAsync(eventId).GetAwaiter().GetResult();

            Assert.AreEqual(0, awardedPoints);
            Assert.AreEqual(5, data.Points);
        }

        [Test]
        public void AddDuplicatePointsAsync_WhenOpenedIdsContainRepeats_AwardsPerOccurrence()
        {
            var eventId = "test";
            var service = CreateService(
                CreateSaveData(
                    eventId,
                    points: 0,
                    ("2", true)),
                CreateCardDefinitions(
                    ("2", 2, false)),
                new DefaultPointsCalculator());

            var awardedPoints = service
                .AddDuplicatePointsAsync(eventId, new[] { "2", "2", "2" })
                .GetAwaiter()
                .GetResult();

            var data = service.LoadAsync(eventId).GetAwaiter().GetResult();

            Assert.AreEqual(6, awardedPoints);
            Assert.AreEqual(6, data.Points);
        }

        [Test]
        public void AddDuplicatePointsAsync_WhenThereAreNoDuplicates_ReturnsZeroWithoutSavingPoints()
        {
            var eventId = "test";
            var service = CreateService(
                CreateSaveData(
                    eventId,
                    points: 11,
                    ("1", false),
                    ("2", false)),
                CreateCardDefinitions(
                    ("1", 1, false),
                    ("2", 2, false)),
                new DefaultPointsCalculator());

            var awardedPoints = service
                .AddDuplicatePointsAsync(eventId, new[] { "1", "2" })
                .GetAwaiter()
                .GetResult();

            var data = service.LoadAsync(eventId).GetAwaiter().GetResult();

            Assert.AreEqual(0, awardedPoints);
            Assert.AreEqual(11, data.Points);
        }

        private static CardProgressService CreateService(
            EventCardsSaveData initialData,
            List<CardDefinition> cardDefinitions,
            ICardPointsCalculator pointsCalculator)
        {
            var service = new CardProgressService(
                new InMemoryEventCardsStorage(initialData),
                new StubCardDefinitionProvider(cardDefinitions),
                pointsCalculator);
            service.InitializeAsync().GetAwaiter().GetResult();
            return service;
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
                    IsUnlocked = isUnlocked
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

        private sealed class DefaultPointsCalculator : ICardPointsCalculator
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

        private sealed class NonPositivePointsCalculator : ICardPointsCalculator
        {
            public int GetPoints(int stars, bool isPremium)
            {
                return 0;
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

        private sealed class InMemoryEventCardsStorage : IEventCardsStorage
        {
            private EventCardsSaveData _data;

            public InMemoryEventCardsStorage(EventCardsSaveData initialData)
            {
                _data = Clone(initialData);
            }

            public Cysharp.Threading.Tasks.UniTask InitializeAsync(System.Threading.CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask<EventCardsSaveData> LoadAsync(string eventId, System.Threading.CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                if (_data == null || _data.EventId != eventId)
                {
                    return Cysharp.Threading.Tasks.UniTask.FromResult(new EventCardsSaveData
                    {
                        EventId = eventId,
                        Version = 1,
                        Points = 0
                    });
                }

                return Cysharp.Threading.Tasks.UniTask.FromResult(Clone(_data));
            }

            public Cysharp.Threading.Tasks.UniTask SaveAsync(EventCardsSaveData data, System.Threading.CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                _data = Clone(data);
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask UnlockCardsAsync(
                EventCardsSaveData data,
                IReadOnlyCollection<string> cardIds,
                System.Threading.CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                foreach (var cardId in cardIds ?? new string[0])
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
                    }
                    else
                    {
                        card.IsUnlocked = true;
                        card.IsNew = true;
                    }
                }

                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask DeleteAsync(string eventId, System.Threading.CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                if (_data != null && _data.EventId == eventId)
                {
                    _data = null;
                }

                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
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
