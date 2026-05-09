using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Crafting
{
    public sealed class SaveBackedCraftingRewardApplier : ICraftingRewardApplier
    {
        private readonly ICraftingInventoryGateway _inventoryGateway;

        public SaveBackedCraftingRewardApplier(ICraftingInventoryGateway inventoryGateway)
        {
            _inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
        }

        public async UniTask ApplyAsync(string outputItemId, int outputCount, CancellationToken ct = default)
        {
            await _inventoryGateway.AddItemAsync(outputItemId, outputCount, ct);
        }
    }
}
