using CardCollection.Core;
using EventOrchestration.Abstractions;
using EventOrchestration.Controllers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplInstaller : LifetimeScope
    {
        [SerializeField] private ExchangePacksConfig _exchangePacksConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_exchangePacksConfig == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(ExchangePacksConfig)} is not assigned on {nameof(CardCollectionImplInstaller)}.");
            }

            builder.RegisterInstance(_exchangePacksConfig);
            
            // Feature storage
            builder.Register<ICardPackProvider, JsonCardPackProvider>(Lifetime.Singleton);
            builder.Register<ICardsConfigProvider, JsonCardsConfigProvider>(Lifetime.Singleton);
            builder.Register<ICardGroupsConfigProvider, JsonCardGroupsConfigProvider>(Lifetime.Singleton);
            
            // Feature session builder
            builder.Register<ICardCollectionRuntimeBuilder, CardCollectionRuntimeBuilder>(Lifetime.Singleton);
            
            // Client code usage facade
            builder.Register<ICardCollectionFeatureFacade, CardCollectionFeatureFacadeFacade>(Lifetime.Singleton);
            
            // TODO add describe
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
