using System.Threading;
using Cysharp.Threading.Tasks;

namespace Inventory.API
{
    public interface IInventoryItemUseHandler 
    {
        bool CanHandle(InventoryItemDelta item);
        UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct);
    }
}
