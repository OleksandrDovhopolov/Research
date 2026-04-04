using CoreResources;
using Game.Bootstrap.Loading;
using Infrastructure.SaveSystem;
using VContainer;

namespace Game.Bootstrap
{
    public class BootstrapInstaller : ScriptableObjectInstaller
    {
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
            
            //Resources
            builder.Register<ResourceManager>(Lifetime.Singleton);
            
            //RemoveConfig
            builder.Register<RemoteConfigLoader>(Lifetime.Singleton);
        }
    }
}