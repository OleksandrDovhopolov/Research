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
    public class CardCollectionApplicationFacadeStage2Tests
    {
        [UnityTest]
        public IEnumerator UnlockCardsUseCase_ReturnsCompletionOutcome() => UniTask.ToCoroutine(async () =>
        {
            const string eventId = "evt";
            var definitions = new List<CardDefinition>
            {
                new() { Id = "a1", GroupType = "group-a", CardName = "a1" }
            };

            var storage = new InMemoryEventCardsStorage(new EventCardsSaveData
            {
                EventId = eventId,
                Cards = new List<CardProgressData>
                {
                    new() { CardId = "a1", IsUnlocked = false }
                }
            });
            var progress = new CardProgressService(storage);
            var useCase = new UnlockCardsUseCase(progress, new StubCardDefinitionProvider(definitions));

            var result = await useCase.ExecuteAsync(eventId, new[] { "a1" }, CancellationToken.None);

            Assert.That(result.UnlockedCardIds, Is.EquivalentTo(new[] { "a1" }));
            Assert.That(result.NewlyCompletedGroupIds, Is.EquivalentTo(new[] { "group-a" }));
            Assert.IsTrue(result.CollectionCompleted);
            Assert.AreEqual(0, result.AwardedDuplicatePoints);
        });

        [UnityTest]
        public IEnumerator Facade_MapsUseCaseOutcome_ToLegacyEvents() => UniTask.ToCoroutine(async () =>
        {
            const string eventId = "evt";
            var storage = new InMemoryEventCardsStorage(new EventCardsSaveData { EventId = eventId });
            var progress = new CardProgressService(storage);
            var points = new PointsAccountService(progress);
            var query = new CollectionProgressQueryService(progress);
            var cardPackService = new CardPackService(new StubPackProvider());

            var facade = new CardCollectionApplicationFacade(
                eventId,
                new StubCardDefinitionProvider(new List<CardDefinition>()),
                cardPackService,
                progress,
                new StubOpenPackUseCase(),
                new StubUnlockCardsUseCase(
                    new UnlockCardsResultDto(new[] { "c1" }, new[] { "g1" }, true, 0)),
                points,
                query);

            int groupEvents = 0;
            int collectionEvents = 0;
            facade.OnGroupCompleted += data => groupEvents += data.Groups?.Count ?? 0;
            facade.OnCollectionCompleted += _ => collectionEvents++;

            await facade.UnlockCards(new[] { "c1" }, CancellationToken.None);

            Assert.AreEqual(1, groupEvents);
            Assert.AreEqual(1, collectionEvents);
        });

        private sealed class StubOpenPackUseCase : IOpenPackUseCase
        {
            public UniTask<OpenPackResultDto> ExecuteAsync(string eventId, string packId, CancellationToken ct = default)
            {
                return UniTask.FromResult(OpenPackResultDto.Empty);
            }
        }

        private sealed class StubUnlockCardsUseCase : IUnlockCardsUseCase
        {
            private readonly UnlockCardsResultDto _result;

            public StubUnlockCardsUseCase(UnlockCardsResultDto result)
            {
                _result = result;
            }

            public UniTask<UnlockCardsResultDto> ExecuteAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                return UniTask.FromResult(_result);
            }
        }

        private sealed class StubPackProvider : ICardPackProvider
        {
            public List<CardPackConfig> Data => new();
            public UniTask<List<CardPackConfig>> LoadAsync(string fileName, CancellationToken ct = default) => UniTask.FromResult(new List<CardPackConfig>());
            public void ClearCache() { }
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

        private sealed class InMemoryEventCardsStorage : IEventCardsStorage
        {
            private EventCardsSaveData _data;

            public InMemoryEventCardsStorage(EventCardsSaveData data)
            {
                _data = data;
            }

            public UniTask InitializeAsync(CancellationToken ct = default) => UniTask.CompletedTask;

            public UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
            {
                _data ??= new EventCardsSaveData { EventId = eventId, Version = 1 };
                return UniTask.FromResult(_data);
            }

            public UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
            {
                _data = data;
                return UniTask.CompletedTask;
            }

            public UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                foreach (var cardId in cardIds ?? new string[0])
                {
                    var card = data.Cards.FirstOrDefault(c => c.CardId == cardId);
                    if (card == null)
                    {
                        data.Cards.Add(new CardProgressData { CardId = cardId, IsUnlocked = true, IsNew = true });
                    }
                    else
                    {
                        card.IsUnlocked = true;
                        card.IsNew = true;
                    }
                }
                return UniTask.CompletedTask;
            }

            public UniTask ClearCollectionAsync(CancellationToken ct = default)
            {
                _data = null;
                return UniTask.CompletedTask;
            }

            public void Dispose() { }
        }
    }
}
