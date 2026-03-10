using Inventory.API;
using UnityEngine;

namespace Inventory.Implementation
{
    public sealed class InventoryImplementationInstaller : MonoBehaviour
    {
        private void Awake()
        {
            if (InventoryCompositionRegistry.IsRegistered)
            {
                return;
            }

            InventoryCompositionRegistry.Register(new InventoryImplementationCompositionRoot());
        }
    }
}
