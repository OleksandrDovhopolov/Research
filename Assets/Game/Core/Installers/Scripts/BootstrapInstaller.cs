using Game.Bootstrap.Loading;
using Infrastructure;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class BootstrapInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private UIManager _uiManagerPrefab;
        [Header("Save Storage")]
        //[SerializeField] private string _httpSaveEndpoint = "http://localhost:5000/api/save/global";
        [SerializeField] private string _httpSaveAuthToken;
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            //Loading
            builder.Register<LoadingProgressAggregator>(_ => new LoadingProgressAggregator(), Lifetime.Singleton);
            builder.Register<IAuthorizationService, MockAuthorizationService>(Lifetime.Singleton);
            builder.Register<LoadingOrchestrator>(Lifetime.Singleton);
            
            //builder.Register<ISaveStorage>(_ => new LocalDiskStorage(), Lifetime.Singleton);
            
            //Save
            builder.Register<ISaveStorage>(_ =>
            {
                var token = string.IsNullOrWhiteSpace(_httpSaveAuthToken) ? null : _httpSaveAuthToken;
                return new HttpSaveStorage(token);
            }, Lifetime.Singleton);
            builder.Register<SaveMigrationService>(Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton);
            
            //RemoveConfig
            builder.Register<RemoteConfigLoader>(Lifetime.Singleton);
            
            //UI
            builder.Register<WindowFactoryDI>(Lifetime.Singleton);
            builder.RegisterComponentInNewPrefab(_uiManagerPrefab, Lifetime.Singleton).DontDestroyOnLoad();
            builder.Register<TransitionAnimationService>(resolver =>
            {
                var uiManager = resolver.Resolve<UIManager>();
                var transitionService = uiManager.GetComponent<TransitionAnimationService>();
                if (transitionService == null)
                {
                    throw new MissingReferenceException($"{nameof(TransitionAnimationService)} is missing on UIManager prefab.");
                }

                return transitionService;
            }, Lifetime.Singleton);
        }
    }
}
