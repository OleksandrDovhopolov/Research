using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryServerApi
    {
        UniTask<InventoryOperationResponse> RemoveAsync(RemoveInventoryItemCommand command, CancellationToken cancellationToken = default);

        UniTask<InventoryOperationResponse> RemoveBatchAsync(RemoveInventoryBatchCommand command, CancellationToken cancellationToken = default);
    }
}
