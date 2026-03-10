using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;

namespace Inventory.Implementation.Services
{
    //TODO made json saved system. use already created classes. 
    internal sealed class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly Dictionary<string, List<InventoryItemView>> _storage = new();

        public UniTask<IReadOnlyList<InventoryItemView>> LoadAsync(
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storage.TryGetValue(ownerId, out var savedItems))
            {
                return UniTask.FromResult((IReadOnlyList<InventoryItemView>)savedItems.ToArray());
            }

            return UniTask.FromResult((IReadOnlyList<InventoryItemView>)new List<InventoryItemView>());
        }

        public UniTask SaveAsync(
            string ownerId,
            IReadOnlyList<InventoryItemView> items,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _storage[ownerId] = new List<InventoryItemView>(items);
            return UniTask.CompletedTask;
        }
    }
}
