using CardCollection.Core;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplInstaller : LifetimeScope
    {
        [SerializeField] private UIManager _uiManager;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_uiManager == null)
            {
                throw new MissingReferenceException($"{nameof(UIManager)} is not assigned on {nameof(CardCollectionImplInstaller)}.");
            }

            builder.RegisterInstance(_uiManager);
            builder.Register<ICardCollectionCompositionRoot, CardCollectionImplCompositionRoot>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container =>
            {
                var compositionRoot = container.Resolve<ICardCollectionCompositionRoot>();
                CardCollectionCompositionRegistry.Register(compositionRoot);
            });
        }
    }
}
