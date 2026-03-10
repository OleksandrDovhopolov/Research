using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryStorage
    {
        Task<IReadOnlyList<InventoryItemView>> LoadAsync(string ownerId, CancellationToken cancellationToken = default);

        Task SaveAsync(string ownerId, IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default);
    }
}
