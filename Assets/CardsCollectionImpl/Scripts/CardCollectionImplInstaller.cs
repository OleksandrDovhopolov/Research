using CardCollection.Core;
using Resources.Core;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplInstaller : LifetimeScope
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private ExchangePacksConfig _exchangePacksConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_uiManager == null)
            {
                throw new MissingReferenceException($"{nameof(UIManager)} is not assigned on {nameof(CardCollectionImplInstaller)}.");
            }

            if (_exchangePacksConfig == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(ExchangePacksConfig)} is not assigned on {nameof(CardCollectionImplInstaller)}.");
            }

            var resourceManager = new ResourceManager();

            builder.RegisterInstance(_uiManager);
            builder.RegisterInstance(_exchangePacksConfig);
            builder.RegisterInstance(resourceManager);
            
            builder.Register<ICardCollectionCompositionRoot, CardCollectionImplCompositionRoot>(Lifetime.Singleton);
            
            builder.RegisterBuildCallback(container =>
            {
                var compositionRoot = container.Resolve<ICardCollectionCompositionRoot>();
                CardCollectionCompositionRegistry.Register(compositionRoot);
            });
        }
    }
}
