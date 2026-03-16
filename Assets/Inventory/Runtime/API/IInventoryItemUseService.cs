using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryItemUseService
    {
        UniTask ConsumeItemAsync(InventoryItemDelta item, CancellationToken cancellationToken = default);
    }
}
