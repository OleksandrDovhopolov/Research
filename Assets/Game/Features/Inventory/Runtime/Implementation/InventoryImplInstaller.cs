using Inventory.API;
using VContainer;
using VContainer.Unity;

namespace Inventory.Implementation
{
    public class InventoryImplInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IInventoryCompositionRoot, InventoryImplementationCompositionRoot>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container =>
            {
                var compositionRoot = container.Resolve<IInventoryCompositionRoot>();
                InventoryCompositionRegistry.Register(compositionRoot);
            });
        }
    }
}
