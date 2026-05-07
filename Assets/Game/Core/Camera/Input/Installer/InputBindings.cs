using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public static class InputBindings
    {
        public static void RegisterInput(this IContainerBuilder builder)
        {
#if UNITY_EDITOR
            builder.Register<MouseInputHandler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#else
            builder.Register<TouchInputHandler>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#endif
            
        }

        private static DragProcessor BuildDrag(IObjectResolver resolver)
        {
            var processor = Create<DragProcessor>(resolver);
            
            var dragFilter = new Filter<IDragInputHandler>();
            dragFilter.Init(Create<CameraDragCommand>(resolver));
            
            processor.SetDragFilter(dragFilter);

            return processor;
        }
        
        private static TapProcessor BuildTap(IObjectResolver resolver)
        {
            var processor = Create<TapProcessor>(resolver);
            return processor;
        }
        
        private static LongTapProcessor BuildLongTap(IObjectResolver resolver)
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