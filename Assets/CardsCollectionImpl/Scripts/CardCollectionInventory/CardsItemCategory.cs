using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardsItemCategory : ItemCategory, IOpenable
    {
        public CardsItemCategory(string categoryId) : base(categoryId, "Card packs")
        {
        }

        public UniTask OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.Log("[Inventory] Opened card pack category item.");
            return UniTask.CompletedTask;
        }

        public override CategoryUiMetadata GetMetadata()
        {
            return new ActionWidgetMetadata();
        }
    }
}
