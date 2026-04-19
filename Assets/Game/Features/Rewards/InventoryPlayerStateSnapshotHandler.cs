using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;

namespace Rewards
{
    public sealed class InventoryPlayerStateSnapshotHandler : IPlayerStateSnapshotHandler
    {
        private readonly IInventorySnapshotService _inventorySnapshotService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IInventoryItemCategoryResolver _itemCategoryResolver;

        public InventoryPlayerStateSnapshotHandler(
            IInventorySnapshotService inventorySnapshotService,
            IPlayerIdentityProvider playerIdentityProvider,
            IInventoryItemCategoryResolver itemCategoryResolver)
        {
            _inventorySnapshotService = inventorySnapshotService ?? throw new System.ArgumentNullException(nameof(inventorySnapshotService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new System.ArgumentNullException(nameof(playerIdentityProvider));
            _itemCategoryResolver = itemCategoryResolver ?? throw new System.ArgumentNullException(nameof(itemCategoryResolver));
        }

        public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot?.InventoryItems == null || snapshot.InventoryItems.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return UniTask.CompletedTask;
            }

            var items = new InventorySnapshotDto();
            for (var i = 0; i < snapshot.InventoryItems.Count; i++)
            {
                var item = snapshot.InventoryItems[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || item.Amount <= 0)
                {
                    continue;
                }

                items.Items.Add(new InventorySnapshotItemDto
                {
                    ItemId = item.ItemId,
                    Amount = item.Amount,
                    CategoryId = _itemCategoryResolver.ResolveCategoryId(item.ItemId)
                });
            }

            return _inventorySnapshotService.ApplySnapshotAsync(items, ct);
        }
    }
}
