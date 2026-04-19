using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using Inventory.API;
using NUnit.Framework;
using R3;

namespace CardCollectionImpl.Tests
{
    public sealed class CardCollectionSettlementCleanupServiceTests
    {
        [Test]
        public void CleanupUnusedPacksAsync_RemovesOnlyEventPacks_AndCalculatesExchange()
        {
            var staticDataLoader = new StubStaticDataLoader(new CardCollectionStaticData
            {
                EventConfig = new EventConfig
                {
                    packs = new List<CardPackConfig>
                    {
                        new() { packId = "pack_a", cardCount = 1, packName = "A" },
                        new() { packId = "pack_b", cardCount = 1, packName = "B" },
                    }
                }
            });

            var readService = new StubInventoryReadService(new List<InventoryItemView>
            {
                new("player_1", "pack_a", 2, CardCollectionGeneralConfig.CardPack),
                new("player_1", "pack_x", 5, CardCollectionGeneralConfig.CardPack),
            });

            var inventoryService = new StubInventoryService(new InventoryBatchRemoveResult(
                requestedStacks: 2,
                removedStacks: 2,
                failedItems: Array.Empty<InventoryItemDelta>()));

            var service = new CardCollectionSettlementCleanupService(
                new StubPlayerIdentityProvider("player_1"),
                inventoryService,
                readService,
                staticDataLoader
                );

            var model = new CardCollectionEventModel
            {
                EventId = "event_1",
                EventConfigAddress = "config_addr",
            };

            var result = service.CleanupUnusedPacksAsync(model, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(inventoryService.LastRemoveRequest.Count, Is.EqualTo(1));
            Assert.That(inventoryService.LastRemoveRequest[0].ItemId, Is.EqualTo("pack_a"));
            Assert.That(inventoryService.LastRemoveRequest[0].Amount, Is.EqualTo(2));
            Assert.That(result.RemovedPacksCount, Is.EqualTo(2));
            Assert.That(result.ExchangeGems, Is.EqualTo(20));
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

        private sealed class StubStaticDataLoader : ICardCollectionStaticDataLoader
        {
            private readonly CardCollectionStaticData _data;

            public StubStaticDataLoader(CardCollectionStaticData data)
            {
                _data = data;
            }

            public UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_data);
            }
        }

        private sealed class StubInventoryReadService : IInventoryReadService
        {
            private readonly IReadOnlyList<InventoryItemView> _items;

            public StubInventoryReadService(IReadOnlyList<InventoryItemView> items)
            {
                _items = items;
            }

            public UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(
                string ownerId,
                string categoryId,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(_items);
            }
        }

        private sealed class StubInventoryService : IInventoryService
        {
            private readonly InventoryBatchRemoveResult _removeResult;
            private readonly Subject<InventoryChangedEvent> _subject = new();

            public StubInventoryService(InventoryBatchRemoveResult removeResult)
            {
                _removeResult = removeResult;
            }

            public List<InventoryItemDelta> LastRemoveRequest { get; private set; } = new();

            public Observable<InventoryChangedEvent> OnInventoryChanged => _subject;

            public UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(
                IReadOnlyList<InventoryItemDelta> itemDeltas,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LastRemoveRequest = itemDeltas == null
                    ? new List<InventoryItemDelta>()
                    : new List<InventoryItemDelta>(itemDeltas);
                return UniTask.FromResult(_removeResult);
            }
        }
    }
}
