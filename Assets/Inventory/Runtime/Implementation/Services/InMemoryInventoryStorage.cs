using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.API;

namespace Inventory.Implementation.Services
{
    //TODO made json saved system. use already created classes. 
    internal sealed class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly Dictionary<string, List<InventoryItemView>> _storage = new();

        public Task<IReadOnlyList<InventoryItemView>> LoadAsync(
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storage.TryGetValue(ownerId, out var savedItems))
            {
                return Task.FromResult((IReadOnlyList<InventoryItemView>)savedItems.ToArray());
            }

            return Task.FromResult((IReadOnlyList<InventoryItemView>)new List<InventoryItemView>());
        }

        public Task SaveAsync(
            string ownerId,
            IReadOnlyList<InventoryItemView> items,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _storage[ownerId] = new List<InventoryItemView>(items);
            return Task.CompletedTask;
        }
    }
}
