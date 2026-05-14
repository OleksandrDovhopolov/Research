using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;

namespace Game.Fishing
{
    public interface IFishingHudLureDataBuilder
    {
        UniTask<IReadOnlyList<FishingHudLureViewData>> BuildAsync(CancellationToken ct = default);
    }

    public sealed class FishingHudLureDataBuilder : IFishingHudLureDataBuilder
    {
        private const string RegularCategoryId = "regular";

        private readonly IFishingConfigProvider _configProvider;
        private readonly IInventoryReadService _inventoryReadService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public FishingHudLureDataBuilder(
            IFishingConfigProvider configProvider,
            IInventoryReadService inventoryReadService,
            IPlayerIdentityProvider playerIdentityProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _inventoryReadService = inventoryReadService ?? throw new ArgumentNullException(nameof(inventoryReadService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }

        public async UniTask<IReadOnlyList<FishingHudLureViewData>> BuildAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var data = await _configProvider.LoadAsync(ct);
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
                throw new InvalidOperationException("Player id is empty.");

            var inventoryItems = await _inventoryReadService.GetItemsAsync(playerId, RegularCategoryId, ct);
            var countsByItemId = BuildCountsByItemId(inventoryItems);

            if (data?.Lures == null || data.Lures.Count == 0)
                return Array.Empty<FishingHudLureViewData>();

            return data.Lures
                .Where(lure => lure != null)
                .OrderBy(lure => lure.SortOrder)
                .ThenBy(lure => lure.Id, StringComparer.Ordinal)
                .Select(lure => new FishingHudLureViewData(
                    lure.Id,
                    lure.DisplayName,
                    lure.Id,
                    lure.CraftRecipeId,
                    GetCount(countsByItemId, lure.ItemId)))
                .ToArray();
        }

        private static Dictionary<string, int> BuildCountsByItemId(IReadOnlyList<InventoryItemView> inventoryItems)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (inventoryItems == null)
                return result;

            for (var i = 0; i < inventoryItems.Count; i++)
            {
                var item = inventoryItems[i];
                if (string.IsNullOrWhiteSpace(item.ItemId) || item.StackCount <= 0)
                    continue;

                if (result.TryGetValue(item.ItemId, out var current))
                    result[item.ItemId] = current + item.StackCount;
                else
                    result[item.ItemId] = item.StackCount;
            }

            return result;
        }

        private static int GetCount(IReadOnlyDictionary<string, int> countsByItemId, string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && countsByItemId.TryGetValue(itemId, out var count)
                ? Math.Max(0, count)
                : 0;
        }
    }
}
