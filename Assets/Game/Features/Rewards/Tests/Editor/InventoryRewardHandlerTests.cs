using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using NUnit.Framework;
using R3;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class InventoryRewardHandlerTests
    {
        [Test]
        public void CanHandle_ReturnsTrueOnlyForInventoryItemKind()
        {
            var handler = new InventoryRewardHandler(new FakeInventoryService(), new StubPlayerIdentityProvider("player_1"));

            Assert.IsTrue(handler.CanHandle(new RewardGrantRequest("Bronze_Pack", RewardKind.InventoryItem, 1, "card_pack")));
            Assert.IsFalse(handler.CanHandle(new RewardGrantRequest("Gold", RewardKind.Resource, 10, "regular")));
        }

        [Test]
        public void HandleAsync_ValidInventoryReward_AddsItemDelta()
        {
            var inventoryService = new FakeInventoryService();
            var handler = new InventoryRewardHandler(inventoryService, new StubPlayerIdentityProvider("player_1"));

            handler.HandleAsync(new RewardGrantRequest("Bronze_Pack", RewardKind.InventoryItem, 2, "card_pack"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual(1, inventoryService.AddCallsCount);
            Assert.AreEqual("player_1", inventoryService.LastAddedItem.OwnerId);
            Assert.AreEqual("Bronze_Pack", inventoryService.LastAddedItem.ItemId);
            Assert.AreEqual(2, inventoryService.LastAddedItem.Amount);
            Assert.AreEqual("card_pack", inventoryService.LastAddedItem.CategoryId);
        }

        [Test]
        public void HandleAsync_AmountIsNotPositive_ThrowsArgumentOutOfRangeException()
        {
            var handler = new InventoryRewardHandler(new FakeInventoryService(), new StubPlayerIdentityProvider("player_1"));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                handler.HandleAsync(new RewardGrantRequest("Bronze_Pack", RewardKind.InventoryItem, 0, "card_pack"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        [Test]
        public void HandleAsync_WhenInventoryAddUnsupported_DoesNotThrow()
        {
            var handler = new InventoryRewardHandler(new UnsupportedInventoryService(), new StubPlayerIdentityProvider("player_1"));

            Assert.DoesNotThrow(() =>
                handler.HandleAsync(new RewardGrantRequest("Bronze_Pack", RewardKind.InventoryItem, 1, "card_pack"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        private sealed class StubPlayerIdentityProvider : IPlayerIdentityProvider
        {
            private readonly string _playerId;

            public StubPlayerIdentityProvider(string playerId)
            {
                _playerId = playerId;
            }

            public string GetPlayerId()
            {
                return _playerId;
            }
        }

        private sealed class FakeInventoryService : IInventoryService
        {
            public Observable<InventoryChangedEvent> OnInventoryChanged { get; } = new Subject<InventoryChangedEvent>();

            public int AddCallsCount { get; private set; }
            public InventoryItemDelta LastAddedItem { get; private set; }

            public UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddCallsCount++;
                LastAddedItem = itemDelta;
                return UniTask.CompletedTask;
            }

            public UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(IReadOnlyList<InventoryItemDelta> itemDeltas, CancellationToken cancellationToken = default)
            {
                var requestedStacks = itemDeltas?.Count ?? 0;
                return UniTask.FromResult(new InventoryBatchRemoveResult(requestedStacks, 0, Array.Empty<InventoryItemDelta>()));
            }
        }

        private sealed class UnsupportedInventoryService : IInventoryService
        {
            public Observable<InventoryChangedEvent> OnInventoryChanged { get; } = new Subject<InventoryChangedEvent>();

            public UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("unsupported");
            }

            public UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(IReadOnlyList<InventoryItemDelta> itemDeltas, CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(new InventoryBatchRemoveResult(0, 0, Array.Empty<InventoryItemDelta>()));
            }
        }
    }
}
