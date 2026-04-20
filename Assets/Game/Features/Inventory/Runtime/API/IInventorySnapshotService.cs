using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventorySnapshotService
    {
        UniTask ApplySnapshotAsync(InventorySnapshotDto snapshot, CancellationToken cancellationToken = default);

        UniTask ApplySnapshotAsync(IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default);
    }
}
