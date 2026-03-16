using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryItemUseService
    {
        UniTask ConsumeItemAsync(InventoryItemDelta item, Action onItemRemoved,CancellationToken cancellationToken = default);
    }
}
