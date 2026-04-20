using System;
using Inventory.API;
using Inventory.Implementation;
using Inventory.Implementation.Services;
using UnityEngine;
using VContainer;

namespace core
{
    public static class InventoryVContainerBindings
    {
        public static void RegisterInventoryService(this IContainerBuilder builder)
        {
            builder.Register<IInventoryServerApi, InventoryServerApi>(Lifetime.Singleton);
            
            builder.Register<IItemCategoryRegistry>(_ =>
            {
                var registry = new ItemCategoryRegistry();
                registry.Register(new SimpleItemCategory());
                return registry;
            }, Lifetime.Singleton);

            builder.Register<InventoryModuleService>(Lifetime.Singleton)
                .As<IInventoryService>()
                .As<IInventoryReadService>()
                .As<IInventoryItemUseService>()
                .As<IInventorySnapshotService>()
                .As<IInventoryUseHandlerStorage>()
                .As<IDisposable>();
        }
    }
}















