using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            Debug.LogWarning($"[CraftingRewardApplier] Adding crafted item to inventory. ItemId='{outputItemId}', Count={outputCount}.");

            try
            {
                await _inventoryGateway.AddItemAsync(outputItemId, outputCount, ct);
                Debug.LogWarning($"[CraftingRewardApplier] Crafted item added to inventory. ItemId='{outputItemId}', Count={outputCount}.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CraftingRewardApplier] Failed to add crafted item to inventory. ItemId='{outputItemId}', Count={outputCount}. {exception}");
                throw;
            }
        }
    }
}
