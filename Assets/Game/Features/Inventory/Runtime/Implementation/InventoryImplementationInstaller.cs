using Inventory.API;
using UnityEngine;

namespace Inventory.Implementation
{
    //TODO delete. real use in InventoryImplInstaller
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
