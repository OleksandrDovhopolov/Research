using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public readonly struct CardCollectionSettlementCleanupResult
    {
        public CardCollectionSettlementCleanupResult(int removedPacksCount, int exchangeGems)
        {
            RemovedPacksCount = removedPacksCount;
            ExchangeGems = exchangeGems;
        }

        public int RemovedPacksCount { get; }
        public int ExchangeGems { get; }
    }

    public interface ICardCollectionSettlementCleanupService
    {
        UniTask<CardCollectionSettlementCleanupResult> CleanupUnusedPacksAsync(
            CardCollectionEventModel model,
            CancellationToken ct);
    }

    public sealed class CardCollectionSettlementCleanupService : ICardCollectionSettlementCleanupService
    {
        private const int GemsPerRemovedPack = 10;

        private readonly string _inventoryOwnerId;
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryReadService _inventoryReadService;
        private readonly ICardCollectionStaticDataLoader _staticDataLoader;

        public CardCollectionSettlementCleanupService(
            string inventoryOwnerId,
            IInventoryService inventoryService,
            IInventoryReadService inventoryReadService,
            ICardCollectionStaticDataLoader staticDataLoader)
        {
            _staticDataLoader = staticDataLoader ?? throw new ArgumentNullException(nameof(staticDataLoader));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _inventoryOwnerId = inventoryOwnerId ?? throw new ArgumentNullException(nameof(inventoryOwnerId));
            _inventoryReadService = inventoryReadService ?? throw new ArgumentNullException(nameof(inventoryReadService));
        }

        public async UniTask<CardCollectionSettlementCleanupResult> CleanupUnusedPacksAsync(
            CardCollectionEventModel model,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (model == null) throw new ArgumentNullException(nameof(model));

            var staticData = await _staticDataLoader.LoadAsync(model, ct);
            var eventPackIds = new HashSet<string>(
                staticData.Packs
                    .Where(pack => !string.IsNullOrWhiteSpace(pack?.packId))
                    .Select(pack => pack.packId),
                StringComparer.Ordinal);

            if (eventPackIds.Count == 0)
            {
                return new CardCollectionSettlementCleanupResult(0, 0);
            }

            var inventoryPacks = await _inventoryReadService.GetItemsAsync(_inventoryOwnerId, CardsConfig.CardPack, ct);
            var removeDeltas = inventoryPacks
                .Where(item => item.StackCount > 0 && eventPackIds.Contains(item.ItemId))
                .Select(item => new InventoryItemDelta(item.OwnerId, item.ItemId, item.StackCount, item.CategoryId))
                .ToList();

            if (removeDeltas.Count == 0)
            {
                return new CardCollectionSettlementCleanupResult(0, 0);
            }

            var removeResult = await _inventoryService.RemoveItemsAsync(removeDeltas, ct);
            var exchangeGems = removeResult.RemovedStacks * GemsPerRemovedPack;
            Debug.LogWarning($"[CardCollectionRuntime] Settlement cleanup removed={removeResult.RemovedStacks}, exchange={exchangeGems}");
            return new CardCollectionSettlementCleanupResult(removeResult.RemovedStacks, exchangeGems);
        }
    }
}
