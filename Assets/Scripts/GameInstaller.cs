using Resources.Core;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace core
{
    public sealed class GameInstaller : LifetimeScope
    {
        [SerializeField] private UIManager _uiManager;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_uiManager == null)
            {
                throw new MissingReferenceException($"{nameof(UIManager)} is not assigned on {nameof(GameInstaller)}.");
            }

            builder.RegisterInstance(_uiManager);
            builder.Register<JsonResourcesStorage>(Lifetime.Singleton);
            builder.Register<ResourceManager>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<Starter>();
        }
    }
}
