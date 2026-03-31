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
        public const string InventoryOwnerId = "player_1";

        public static void RegisterInventoryService(this IContainerBuilder builder)
        {
            builder.RegisterInstance(InventoryOwnerId);

            builder.Register<IInventoryStorage, InMemoryInventoryStorage>(Lifetime.Singleton);
            
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
                .As<IInventoryUseHandlerStorage>()
                .As<IDisposable>();
        }
    }
}















