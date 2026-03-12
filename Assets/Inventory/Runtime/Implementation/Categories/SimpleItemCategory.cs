using Inventory.API;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Inventory.Implementation
{
    public sealed class SimpleItemCategory : ItemCategory, IConsumable
    {
        public SimpleItemCategory()
            : base(InventorySharedConfigs.Regular, "Regular")
        {
        }

        public UniTask ConsumeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("[Inventory] Consumed item from regular category.");
            return UniTask.CompletedTask;
        }
    }
}
