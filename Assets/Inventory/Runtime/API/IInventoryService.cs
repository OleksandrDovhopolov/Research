using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Inventory.API
{
    public interface IInventoryService
    {
        Observable<InventoryChangedEvent> OnInventoryChanged { get; }

        UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);

        UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);

        //TODO GetItemsAsync should be another interface IInventoryReadService
        UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(
            string ownerId,
            InventoryItemCategory category,
            CancellationToken cancellationToken = default);
    }
}
