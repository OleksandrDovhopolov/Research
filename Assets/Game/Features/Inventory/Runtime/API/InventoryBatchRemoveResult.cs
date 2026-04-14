using System;
using System.Collections.Generic;

namespace Inventory.API
{
    public readonly struct InventoryBatchRemoveResult
    {
        public InventoryBatchRemoveResult(
            int requestedStacks,
            int removedStacks,
            IReadOnlyList<InventoryItemDelta> failedItems)
        {
            RequestedStacks = requestedStacks;
            RemovedStacks = removedStacks;
            FailedItems = failedItems ?? Array.Empty<InventoryItemDelta>();
        }

        public int RequestedStacks { get; }
        public int RemovedStacks { get; }
        public IReadOnlyList<InventoryItemDelta> FailedItems { get; }
    }
}
