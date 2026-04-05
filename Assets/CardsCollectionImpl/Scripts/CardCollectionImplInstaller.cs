using CardCollection.Core;
using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using UnityEngine;
using VContainer;

namespace CardCollectionImpl
{
    public static class CardCollectionImplInstaller
    {
        public static void RegisterCardCollectionImpl(this IContainerBuilder builder)
        {
            Debug.LogWarning($"[Debug] CardCollectionImplInstaller started");
            builder.Register<IEventConfigProvider, FirebaseEventConfigProvider>(Lifetime.Singleton);
            
            builder.Register<IEventCardsStorage, JsonEventCardsStorage>(Lifetime.Singleton);
            builder.Register<IPackSelectionStrategy, DefaultPackStrategy>(Lifetime.Singleton);
            builder.Register<ICardSelector, RandomCardSelector>(Lifetime.Singleton);
            // Points calculator
            builder.Register<ICardCollectionCacheService, CardCollectionCardCollectionCacheService>(Lifetime.Singleton);
            builder.Register<ICardPointsCalculator, CardsCollectionPointsCalculator>(Lifetime.Singleton);
            builder.Register<EventSpriteManager>(Lifetime.Singleton)
                .As<IEventSpriteManager>()
                .As<System.IDisposable>();
            
            // Feature session builder
            builder.Register<ICardCollectionStaticDataLoader, CardCollectionStaticDataLoader>(Lifetime.Singleton);
            builder.Register<ICardCollectionApplicationFacadeFactory, CardCollectionApplicationFacadeFactory>(Lifetime.Singleton);
            builder.Register<ICardCollectionSessionFactory, CardCollectionSessionFactory>(Lifetime.Singleton);
            builder.Register<ICardCollectionRuntimeBuilder, CardCollectionRuntimeBuilder>(Lifetime.Singleton);
            
            // Client code usage facade
            builder.Register<ICardCollectionSessionFacade, CardCollectionSessionFacade>(Lifetime.Singleton);
            
            // Event model factory
            builder.Register<IEventModelFactory, CardCollectionEventModelFactory>(Lifetime.Singleton);
            
            // Feature lifeOps controller
            builder.Register<CardCollectionLiveOpsController>(Lifetime.Singleton);
            
            builder.RegisterBuildCallback(container =>
            {
                var eventRegistry = container.Resolve<IEventRegistry>();
                var controller = container.Resolve<CardCollectionLiveOpsController>();
                eventRegistry.Register(controller);
            });
            Debug.LogWarning($"[Debug] CardCollectionImplInstaller completed");
        }
    }
}
