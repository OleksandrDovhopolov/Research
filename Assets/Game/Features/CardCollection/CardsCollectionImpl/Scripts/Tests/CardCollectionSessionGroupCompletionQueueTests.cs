using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public class CardCollectionSessionGroupCompletionQueueTests
    {
        [Test]
        public void OnGroupCompleted_EnqueuesGroupImmediately_BeforeRewardGrantCompletes()
        {
            var queue = new PendingGroupCompletionPresentationQueue(new[]
            {
                new CardCollectionGroupConfig { groupType = "group-a", groupName = "Group A" }
            });
            var rewardHandler = new PendingRewardHandler();
            var context = new CardCollectionSessionContext(
                new NoOpOpenPackFlow(),
                new NoOpWindowCoordinator(),
                new NoOpCardCollectionApplicationFacade(),
                queue);

            var session = CreateSession();
            SetPrivateField(session, "<Context>k__BackingField", context);
            SetPrivateField(session, "_rewardHandler", rewardHandler);
            SetPrivateField(session, "_cts", new CancellationTokenSource());
            SetPrivateField(session, "_isStarted", true);
            SetPrivateField(session, "_isDisposed", false);

            InvokePrivateMethod(session, "OnGroupCompleted", new CardGroupsCompletedData(new[]
            {
                new CardGroupCompletedData { GroupType = "group-a" }
            }));

            var pendingGroups = queue.DequeueAll();
            Assert.AreEqual(1, pendingGroups.Count);
            Assert.AreEqual("group-a", pendingGroups[0].groupType);

            rewardHandler.Complete(true);
        }

        private static CardCollectionSession CreateSession()
        {
            return (CardCollectionSession)FormatterServices.GetUninitializedObject(typeof(CardCollectionSession));
        }

        private static void InvokePrivateMethod(object target, string methodName, object argument)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Method '{methodName}' was not found.");
            method!.Invoke(target, new[] { argument });
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field!.SetValue(target, value);
        }

        private sealed class PendingRewardHandler : ICardCollectionRewardHandler
        {
            private readonly UniTaskCompletionSource<bool> _groupRewardPending = new();

            public void Complete(bool value)
            {
                _groupRewardPending.TrySetResult(value);
            }

            public RewardViewData CreateRewardViewData(string groupType)
            {
                return RewardViewData.Empty;
            }

            public UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default)
            {
                return _groupRewardPending.Task;
            }

            public UniTask<bool> TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData, CancellationToken ct = default)
            {
                return UniTask.FromResult(true);
            }

            public UniTask<bool> TryHandleBuyPointsOffer(string offerId, CancellationToken ct = default)
            {
                return UniTask.FromResult(true);
            }
        }

        private sealed class NoOpOpenPackFlow : IOpenPackFlow
        {
            public UniTask OpenPackById(string packId, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask ShowPendingGroupCompletedAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }

        private sealed class NoOpWindowCoordinator : ICardCollectionWindowCoordinator
        {
            public void ShowStarted(CollectionStartedArgs args) { }
            public void ShowCompleted(CollectionCompletedArgs args) { }
            public void ShowCollection(CardCollectionArgs args) { }
            public void ShowNewCard(NewCardArgs args) { }
            public void ShowGroupCompleted(CardGroupCollectionArgs args) { }
            public void CloseSessionWindows() { }
        }

        private sealed class NoOpCardCollectionApplicationFacade : ICardCollectionApplicationFacade
        {
            public string EventId => "event-test";
            public event Action<CardGroupsCompletedData> OnGroupCompleted;
            public event Action<CardCollectionCompletedData> OnCollectionCompleted;

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
                return UniTask.CompletedTask;
            }

            public UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new EventCardsSaveData());
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
