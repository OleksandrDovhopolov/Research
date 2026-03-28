using CardCollection.Core;
using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using UnityEngine;
using VContainer;

namespace CardCollectionImpl
{
    public static class CardCollectionImplInstaller
    {
        public static void RegisterCardCollectionImpl(this IContainerBuilder builder, ExchangePacksConfig exchangePacksConfig)
        {
            if (exchangePacksConfig == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(ExchangePacksConfig)} is not assigned on {nameof(CardCollectionImplInstaller)}.");
            }

            builder.RegisterInstance(exchangePacksConfig);
            
            //TODO this should be in CardCollectionImplInstaller. but CardCollectionController crashes because cant resolve 
            // dependencies from CardCollectionImplInstaller. Bug in WindowFactoryDI -  var controller = _diContainer.Resolve<T>();
            // Card collection feature storage
            builder.Register<ICardPackProvider, JsonCardPackProvider>(Lifetime.Singleton);
            builder.Register<ICardsConfigProvider, JsonCardsConfigProvider>(Lifetime.Singleton);
            builder.Register<ICardGroupsConfigProvider, JsonCardGroupsConfigProvider>(Lifetime.Singleton);
            // Points calculator
            builder.Register<ICardCollectionCacheService, CardCollectionCardCollectionCacheService>(Lifetime.Singleton);
            builder.Register<ICardPointsCalculator, CardsCollectionPointsCalculator>(Lifetime.Singleton);
            
            // Feature session builder
            builder.Register<ICardCollectionRuntimeBuilder, CardCollectionRuntimeBuilder>(Lifetime.Singleton);
            
            // Client code usage facade
            builder.Register<ICardCollectionFeatureFacade, CardCollectionFeatureFacadeFacade>(Lifetime.Singleton);
            
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
        }
    }
}
