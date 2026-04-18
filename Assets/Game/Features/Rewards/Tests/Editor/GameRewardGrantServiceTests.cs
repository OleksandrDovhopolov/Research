using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class GameRewardGrantServiceTests
    {
        [Test]
        public void TryGrantAsync_AllRewardsHandled_ReturnsTrue()
        {
            var resourceHandler = new SpyRewardHandler(RewardKind.Resource);
            var inventoryHandler = new SpyRewardHandler(RewardKind.InventoryItem);
            var service = new GameRewardGrantService(new List<IRewardHandler> { resourceHandler, inventoryHandler });

            var rewards = new List<RewardGrantRequest>
            {
                new("Gold", RewardKind.Resource, 10, "regular"),
                new("CardPack", RewardKind.InventoryItem, 1, "card_pack")
            };

            var result = service.TryGrantAsync(rewards, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, resourceHandler.HandleCallsCount);
            Assert.AreEqual(1, inventoryHandler.HandleCallsCount);
        }

        [Test]
        public void TryGrantAsync_InvalidReward_ContinuesBatchAndReturnsFalse()
        {
            var resourceHandler = new SpyRewardHandler(RewardKind.Resource);
            var service = new GameRewardGrantService(new List<IRewardHandler> { resourceHandler });

            var rewards = new List<RewardGrantRequest>
            {
                new("Invalid", RewardKind.Unknown, 10, "regular"),
                new("Gold", RewardKind.Resource, 15, "regular")
            };

            var result = service.TryGrantAsync(rewards, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result);
            Assert.AreEqual(1, resourceHandler.HandleCallsCount);
            Assert.AreEqual("Gold", resourceHandler.LastRequest.RewardId);
        }

        [Test]
        public void TryGrantAsync_UnsupportedReward_ContinuesBatchAndReturnsFalse()
        {
            var resourceHandler = new SpyRewardHandler(RewardKind.Resource);
            var service = new GameRewardGrantService(new List<IRewardHandler> { resourceHandler });

            var rewards = new List<RewardGrantRequest>
            {
                new("CardPack", RewardKind.InventoryItem, 1, "card_pack"),
                new("Gold", RewardKind.Resource, 5, "regular")
            };

            var result = service.TryGrantAsync(rewards, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result);
            Assert.AreEqual(1, resourceHandler.HandleCallsCount);
            Assert.AreEqual("Gold", resourceHandler.LastRequest.RewardId);
        }

        [Test]
        public void TryGrantAsync_HandlerThrows_ContinuesBatchAndReturnsFalse()
        {
            var throwingHandler = new SpyRewardHandler(RewardKind.Resource, shouldThrow: true);
            var inventoryHandler = new SpyRewardHandler(RewardKind.InventoryItem);
            var service = new GameRewardGrantService(new List<IRewardHandler> { throwingHandler, inventoryHandler });

            var rewards = new List<RewardGrantRequest>
            {
                new("Gold", RewardKind.Resource, 5, "regular"),
                new("CardPack", RewardKind.InventoryItem, 1, "card_pack")
            };

            var result = service.TryGrantAsync(rewards, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result);
            Assert.AreEqual(1, throwingHandler.HandleCallsCount);
            Assert.AreEqual(1, inventoryHandler.HandleCallsCount);
        }

        [Test]
        public void TryGrantAsync_CancelledBeforeStart_ThrowsOperationCanceledException()
        {
            var resourceHandler = new SpyRewardHandler(RewardKind.Resource);
            var service = new GameRewardGrantService(new List<IRewardHandler> { resourceHandler });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                service.TryGrantAsync(
                        new List<RewardGrantRequest> { new("Gold", RewardKind.Resource, 5, "regular") },
                        cts.Token)
                    .GetAwaiter()
                    .GetResult());
        }

        [Test]
        public void TryGrantAsync_CancelledDuringBatch_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            var firstHandler = new SpyRewardHandler(RewardKind.Resource, onHandle: () => cts.Cancel());
            var secondHandler = new SpyRewardHandler(RewardKind.InventoryItem);
            var service = new GameRewardGrantService(new List<IRewardHandler> { firstHandler, secondHandler });

            var rewards = new List<RewardGrantRequest>
            {
                new("Gold", RewardKind.Resource, 1, "regular"),
                new("CardPack", RewardKind.InventoryItem, 1, "card_pack")
            };

            Assert.Throws<OperationCanceledException>(() =>
                service.TryGrantAsync(rewards, cts.Token).GetAwaiter().GetResult());

            Assert.AreEqual(1, firstHandler.HandleCallsCount);
            Assert.AreEqual(0, secondHandler.HandleCallsCount);
        }

        private sealed class SpyRewardHandler : IRewardHandler
        {
            private readonly RewardKind _kind;
            private readonly bool _shouldThrow;
            private readonly Action _onHandle;

            public SpyRewardHandler(RewardKind kind, bool shouldThrow = false, Action onHandle = null)
            {
                _kind = kind;
                _shouldThrow = shouldThrow;
                _onHandle = onHandle;
            }

            public int HandleCallsCount { get; private set; }
            public RewardGrantRequest LastRequest { get; private set; }

            public bool CanHandle(RewardGrantRequest request)
            {
                return request != null && request.Kind == _kind;
            }

            public UniTask HandleAsync(RewardGrantRequest request, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                HandleCallsCount++;
                LastRequest = request;
                _onHandle?.Invoke();

                if (_shouldThrow)
                {
                    throw new InvalidOperationException("Simulated reward handler failure.");
                }

                return UniTask.CompletedTask;
            }
        }
    }
}
