using Inventory.API;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Inventory.Implementation
{
    public sealed class CardsItemCategory : ItemCategory, IOpenable
    {
        public CardsItemCategory()
            : base(InventorySharedConfigs.CardPack, "Card packs")
        {
        }

        public UniTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("[Inventory] Opened card pack category item.");
            return UniTask.CompletedTask;
        }
    }
}
