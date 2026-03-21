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
            
            builder.Register<ICardCollectionCompositionRoot, CardCollectionImplCompositionRoot>(Lifetime.Singleton);
            builder.Register<ICardCollectionFeatureFacade, CardCollectionFeatureFacadeFacade>(Lifetime.Singleton);
            builder.Register<IEventModelFactory, CardCollectionEventModelFactory>(Lifetime.Singleton);
            builder.Register<CardCollectionLiveOpsController>(Lifetime.Singleton);
            
            builder.RegisterBuildCallback(container =>
            {
                var compositionRoot = container.Resolve<ICardCollectionCompositionRoot>();
                CardCollectionCompositionRegistry.Register(compositionRoot);

                var eventRegistry = container.Resolve<IEventRegistry>();
                var controller = container.Resolve<CardCollectionLiveOpsController>();
                eventRegistry.Register(controller);
            });
        }
    }
}
