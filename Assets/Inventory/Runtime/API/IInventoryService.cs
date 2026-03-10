using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Inventory.API
{
    public interface IInventoryService
    {
        Observable<InventoryChangedEvent> OnInventoryChanged { get; }

        Task AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);

        Task RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);

        //TODO GetItemsAsync should be another interface IInventoryReadService
        Task<IReadOnlyList<InventoryItemView>> GetItemsAsync(
            string ownerId,
            InventoryItemCategory category,
            CancellationToken cancellationToken = default);
    }
}
