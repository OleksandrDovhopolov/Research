using Game.Bootstrap.Loading;
using Analytics;
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
        [Header("Analytics")]
        [SerializeField] private AnalyticsConfigSO _analyticsConfig;
        [SerializeField] private AnalyticsRoutingConfigSO _analyticsRoutingConfig;
        [SerializeField] private AnalyticsMappingConfigSO _analyticsMappingConfig;
        [Header("Save Storage")]
        [SerializeField] private string _httpSaveAuthToken;
        [SerializeField] private bool _enableLocalDebugSaveMirror;
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            //Loading
            builder.Register<LoadingProgressAggregator>(_ => new LoadingProgressAggregator(), Lifetime.Singleton);
            builder.Register<IAuthorizationService, MockAuthorizationService>(Lifetime.Singleton);
            builder.Register<LoadingOrchestrator>(Lifetime.Singleton);
            
            //Save
            builder.Register<IPlayerIdentityProvider, PersistentInstallPlayerIdentityProvider>(Lifetime.Singleton);
            //builder.Register<ISaveStorage>(_ => new LocalDiskStorage(), Lifetime.Singleton);
            builder.Register<ISaveStorage>(_ =>
            {
                var token = string.IsNullOrWhiteSpace(_httpSaveAuthToken) ? null : _httpSaveAuthToken;
                var playerIdentityProvider = _.Resolve<IPlayerIdentityProvider>();
                return new HttpSaveStorage(token, playerIdentityProvider);
            }, Lifetime.Singleton);
            builder.Register<ISaveDebugMirror>(_ =>
            {
                return _enableLocalDebugSaveMirror
                    ? new LocalDiskSaveDebugMirror()
                    : new NullSaveDebugMirror();
            }, Lifetime.Singleton);
            builder.Register<SaveMigrationService>(Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton);
            
            //Analytics
            builder.RegisterAnalytics(_analyticsConfig, _analyticsRoutingConfig, _analyticsMappingConfig);
            
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
