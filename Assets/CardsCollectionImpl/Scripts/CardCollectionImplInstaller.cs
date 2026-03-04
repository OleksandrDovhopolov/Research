using CardCollection.Core;
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
            
            builder.RegisterBuildCallback(container =>
            {
                var compositionRoot = container.Resolve<ICardCollectionCompositionRoot>();
                CardCollectionCompositionRegistry.Register(compositionRoot);
            });
        }
    }
}
