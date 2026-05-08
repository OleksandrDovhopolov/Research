using Game.Bootstrap;
using Game.Features.Locations;
using VContainer;

namespace InputSystem
{
    public class InputInstaller : MonoInstaller
    {
        public override void InstallBindings(IContainerBuilder builder)
        {
#if UNITY_EDITOR
            builder.Register<MouseInputHandler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#else
            builder.Register<TouchInputHandler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#endif

            builder.Register<LocationRaycaster>(Lifetime.Singleton);
            builder.Register<LocationInteractionRouter>(Lifetime.Singleton);
            builder.Register<DefaultLocationInteractionUiGateway>(Lifetime.Singleton).As<ILocationInteractionUiGateway>();
            builder.Register<FishingZoneInteractionHandler>(Lifetime.Singleton);
            builder.Register<FisherHouseInteractionHandler>(Lifetime.Singleton);
            builder.Register<ChestInteractionHandler>(Lifetime.Singleton);
            builder.Register<FishCollectionInteractionHandler>(Lifetime.Singleton);
            builder.Register<UnknownLocationInteractionHandler>(Lifetime.Singleton);

            builder.RegisterBuildCallback(OnBuildCallback);
        }

        private void OnBuildCallback(IObjectResolver resolver)
        {
            var inputHandler = resolver.Resolve<IInput>();
            var router = resolver.Resolve<LocationInteractionRouter>();
            router.RegisterHandler(LocationInteractionKeys.FishingZone, resolver.Resolve<FishingZoneInteractionHandler>());
            router.RegisterHandler(LocationInteractionKeys.FisherHouse, resolver.Resolve<FisherHouseInteractionHandler>());
            router.RegisterHandler(LocationInteractionKeys.Chest, resolver.Resolve<ChestInteractionHandler>());
            router.RegisterHandler(LocationInteractionKeys.FishCollection, resolver.Resolve<FishCollectionInteractionHandler>());
            router.RegisterFallbackHandler(resolver.Resolve<UnknownLocationInteractionHandler>());

            inputHandler.TapProcessorHandler = BuildTap(resolver);
            inputHandler.LongTapProcessorHandler = BuildLongTap(resolver);
            inputHandler.DragProcessorHandler = BuildDrag(resolver);
        }

        private DragProcessor BuildDrag(IObjectResolver resolver)
        {
            var processor = Create<DragProcessor>(resolver);

            var dragFilter = new Filter<IDragInputHandler>();
            dragFilter.Init(Create<CameraDragCommand>(resolver));

            processor.SetDragFilter(dragFilter);

            return processor;
        }

        private TapProcessor BuildTap(IObjectResolver resolver)
        {
            var processor = Create<TapProcessor>(resolver);

            processor.AddCommand(Create<LocationObjectTapCommand>(resolver))
                .WithFilter<ObjectFilterNode<ILocationInteractable>>()
                .Build();

            return processor;
        }

        private LongTapProcessor BuildLongTap(IObjectResolver resolver)
        {
            var processor = Create<LongTapProcessor>(resolver);
            return processor;
        }

        private static T Create<T>(IObjectResolver resolver) where T : new()
        {
            var result = new T();
            resolver.Inject(result);
            return result;
        }
    }
}
