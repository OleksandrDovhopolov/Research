using Inventory.API;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Inventory.Implementation
{
    public sealed class SimpleItemCategory : ItemCategory, IConsumable
    {
        public const string Regular = "regular";
        
        public SimpleItemCategory() : base(Regular, "Regular")
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
