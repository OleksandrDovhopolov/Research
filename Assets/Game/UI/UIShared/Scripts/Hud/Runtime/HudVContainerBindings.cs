using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;

namespace UIShared
{
    public static class HudVContainerBindings
    {
        public static void RegisterHud(this IContainerBuilder builder, HudRoot hudRoot, HudWidgetRegistryAsset hudWidgetRegistry, CancellationToken cancellationToken)
        {
            builder.RegisterInstance(hudRoot);
            builder.RegisterInstance(hudWidgetRegistry);
            builder.Register<IHudPrefabLoader, AddressablesHudPrefabLoader>(Lifetime.Singleton);
            builder.Register<HudMissTapInputController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<IHudController>(
                resolver => new HudController(
                    hudRoot,
                    hudWidgetRegistry,
                    resolver.Resolve<IHudPrefabLoader>(),
                    resolver),
                Lifetime.Singleton);

            builder.RegisterBuildCallback(resolver => resolver.InitializeHud(cancellationToken));
        }

        public static IHudController InitializeHud(this IObjectResolver resolver, CancellationToken cancellationToken)
        {
            var hudController = resolver.Resolve<IHudController>();
            hudController.CreateInitialWidgetsAsync(cancellationToken).Forget();
            return hudController;
        }
    }
}
