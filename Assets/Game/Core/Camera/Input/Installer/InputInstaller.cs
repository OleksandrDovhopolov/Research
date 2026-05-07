using Game.Bootstrap;
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
            
            //builder.Register<Raycaster>(Lifetime.Singleton);
            builder.RegisterBuildCallback(OnBuildCallback);
        }
        
        private void OnBuildCallback(IObjectResolver resolver)
        {
            var inputHandler = resolver.Resolve<IInput>();
            
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