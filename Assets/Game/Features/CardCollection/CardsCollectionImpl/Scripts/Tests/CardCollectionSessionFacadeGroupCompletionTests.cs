using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public class CardCollectionSessionFacadeGroupCompletionTests
    {
        [Test]
        public void TryUnlockCards_ShowsPendingGroupCompletedOnce()
        {
            var facade = new CardCollectionSessionFacade();
            var openPackFlow = new FakeOpenPackFlow();
            var appFacade = new FakeCardCollectionApplicationFacade(new EventCardsSaveData());
            var context = CreateContext(openPackFlow, appFacade);
            SetActiveContext(facade, context);

            facade.TryUnlockCards(new[] { "card-10" }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, appFacade.UnlockCardsCallsCount);
            CollectionAssert.AreEqual(new[] { "card-10" }, appFacade.UnlockCalls.SelectMany(ids => ids));
            Assert.AreEqual(1, openPackFlow.ShowPendingCallsCount);
        }

        [Test]
        public void TryCompleteAllCollection_ShowsPendingGroupCompletedOnceAfterBatch()
        {
            var facade = new CardCollectionSessionFacade();
            var openPackFlow = new FakeOpenPackFlow();
            var appFacade = new FakeCardCollectionApplicationFacade(new EventCardsSaveData
            {
                Cards = new List<CardProgressData>
                {
                    new() { CardId = "c1" },
                    new() { CardId = "c2" },
                    new() { CardId = "c2" },
                    new() { CardId = string.Empty },
                    new() { CardId = null },
                    new() { CardId = "c3" },
                }
            });

            var context = CreateContext(openPackFlow, appFacade);
            SetActiveContext(facade, context);

            facade.TryCompleteAllCollection(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(3, appFacade.UnlockCardsCallsCount);
            CollectionAssert.AreEqual(new[] { "c1", "c2", "c3" }, appFacade.UnlockCalls.SelectMany(ids => ids));
            Assert.AreEqual(1, openPackFlow.ShowPendingCallsCount);
        }

        private static CardCollectionSessionContext CreateContext(
            FakeOpenPackFlow openPackFlow,
            FakeCardCollectionApplicationFacade appFacade)
        {
            var queue = new PendingGroupCompletionPresentationQueue(Array.Empty<CardCollectionGroupConfig>());
            return new CardCollectionSessionContext(openPackFlow, new FakeWindowCoordinator(), appFacade, queue);
        }

        private static void SetActiveContext(CardCollectionSessionFacade facade, CardCollectionSessionContext context)
        {
            var setActiveSessionMethod = typeof(CardCollectionSessionFacade)
                .GetMethod("CardCollectionImpl.ICardCollectionSessionFacade.SetActiveSession",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(setActiveSessionMethod, "Could not find explicit SetActiveSession implementation.");
            setActiveSessionMethod!.Invoke(facade, new object[] { context });
        }

        private sealed class FakeOpenPackFlow : IOpenPackFlow
        {
            public int ShowPendingCallsCount { get; private set; }

            public UniTask OpenPackById(string packId, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask ShowPendingGroupCompletedAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                ShowPendingCallsCount++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeWindowCoordinator : ICardCollectionWindowCoordinator
        {
            public void ShowStarted(CollectionStartedArgs args) { }
            public void ShowCompleted(CollectionCompletedArgs args) { }
            public void ShowCollection(CardCollectionArgs args) { }
            public void ShowNewCard(NewCardArgs args) { }
            public void ShowGroupCompleted(CardGroupCollectionArgs args) { }
            public void CloseSessionWindows() { }
        }

        private sealed class FakeCardCollectionApplicationFacade : ICardCollectionApplicationFacade
        {
            private readonly EventCardsSaveData _loadData;

            public string EventId => "event-test";
            public int UnlockCardsCallsCount { get; private set; }
            public List<IReadOnlyCollection<string>> UnlockCalls { get; } = new();

            public event Action<CardGroupsCompletedData> OnGroupCompleted;
            public event Action<CardCollectionCompletedData> OnCollectionCompleted;

            public FakeCardCollectionApplicationFacade(EventCardsSaveData loadData)
            {
                _loadData = loadData;
            }

            public UniTask<OpenPackResultDto> OpenPackAsync(string packId, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(OpenPackResultDto.Empty);
            }

            public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new List<CardProgressData>());
            }

            public UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask UnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                UnlockCardsCallsCount++;
                UnlockCalls.Add(cardIds?.ToArray() ?? Array.Empty<string>());
                return UniTask.CompletedTask;
            }

            public UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_loadData);
            }

            public UniTask PurgeEventDataAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(true);
            }

            public UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(true);
            }

            public UniTask<int> GetCollectionPoints(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(0);
            }

            public void Dispose()
            {
                OnGroupCompleted = null;
                OnCollectionCompleted = null;
            }
        }
    }
}
