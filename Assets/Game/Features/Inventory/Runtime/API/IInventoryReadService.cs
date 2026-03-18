using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryReadService
    {
        UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(
            string ownerId,
            string categoryId,
            CancellationToken cancellationToken = default);
    }
}
