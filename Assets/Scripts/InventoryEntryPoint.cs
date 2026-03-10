using System;
using Inventory.API;
using UnityEngine;

namespace core
{
    public sealed class InventoryEntryPoint : MonoBehaviour
    {
        private IInventoryService _inventoryService;

        public IInventoryService InventoryService => GetInitializedService();

        private IInventoryService GetInitializedService()
        {
            if (_inventoryService == null)
            {
                throw new InvalidOperationException(
                    "Inventory service is not initialized. Ensure inventory installer runs before this entry point.");
            }

            return _inventoryService;
        }

        private void Awake()
        {
            _inventoryService = InventoryCompositionRegistry.Resolve().CreateInventoryService();
        }
    }
}
