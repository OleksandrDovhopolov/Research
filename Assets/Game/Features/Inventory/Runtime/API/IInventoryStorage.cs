using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryStorage
    {
        UniTask<IReadOnlyList<InventoryItemView>> LoadAsync(string ownerId, CancellationToken cancellationToken = default);

        UniTask SaveAsync(string ownerId, IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default);
    }
}
