using core;
using Game.Bootstrap.Loading;
using Infrastructure.SaveSystem;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class BootstrapInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private UIManager _uiManagerPrefab;
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            //Loading
            builder.Register<LoadingProgressAggregator>(_ => new LoadingProgressAggregator(), Lifetime.Singleton);
            builder.Register<IAuthorizationService, MockAuthorizationService>(Lifetime.Singleton);
            builder.Register<LoadingOrchestrator>(Lifetime.Singleton);
            
            //Save
            builder.Register<ISaveStorage>(_ => new LocalDiskStorage(), Lifetime.Singleton);
            builder.Register<SaveMigrationService>(Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton);
            
            //RemoveConfig
            builder.Register<RemoteConfigLoader>(Lifetime.Singleton);
            
            //UI
            builder.Register<WindowFactoryDI>(Lifetime.Singleton);
            builder.RegisterComponentInNewPrefab(_uiManagerPrefab, Lifetime.Singleton).DontDestroyOnLoad();
        }
    }
}