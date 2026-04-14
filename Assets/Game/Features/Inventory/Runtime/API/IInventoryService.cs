using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;

namespace Inventory.API
{
    public interface IInventoryService
    {
        Observable<InventoryChangedEvent> OnInventoryChanged { get; }

        UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);

        UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default);
        
        UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(
            IReadOnlyList<InventoryItemDelta> itemDeltas,
            CancellationToken cancellationToken = default);
    }
}
