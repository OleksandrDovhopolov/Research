using System.Collections.Generic;
using BattlePass;
using CameraModule;
using CardCollectionImpl;
using core;
using CoreResources;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using FortuneWheel;
using Game.Features.Locations;
using Infrastructure;
using InputSystem;
using Rewards;
using UIShared;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using ApiConfig = Infrastructure.ApiConfig;

namespace Game.Bootstrap
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private GlobalTimerService _globalTimerService;
        [SerializeField] private RewardSpecsConfigSO _rewardSpecsConfigSo;
        [SerializeField] private RewardedAdsConfigSO _rewardedAdsConfigSo;
        [SerializeField] private string _liveOpsScheduleFile = "liveops_schedule.json";
        [SerializeField] private string _liveOpsScheduleConfigName = "cards_event_schedule";
        
        [SerializeField] private CameraSettings _cameraSettings;
        [SerializeField] private CameraBehaviour _cameraBehaviour;
        
        [Space, Header("Location")]
        [SerializeField] private LocationZoneInfoHudBootstrap _locationZoneInfoHudBootstrap;
        [SerializeField] private MainLocationBootstrap _mainLocationBootstrap;

        [Space, Header("HUD")]
        [SerializeField] private HudRoot _hudRoot;
        [SerializeField] private HudWidgetRegistryAsset _hudWidgetRegistry;
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            if (_globalTimerService == null)
            {
                throw new MissingReferenceException($"{nameof(GlobalTimerService)} is not assigned on {nameof(GameInstaller)}.");
            }
            
            var rewardSpecsConfig = _rewardSpecsConfigSo != null
                ? _rewardSpecsConfigSo
                : ScriptableObject.CreateInstance<RewardSpecsConfigSO>();

            //Camera
            builder.Register<ScreenPointConverter>(Lifetime.Singleton);
            builder.RegisterInstance(_cameraSettings);
            builder.RegisterComponent(_cameraBehaviour);

            // Location
            builder.RegisterComponent(_mainLocationBootstrap);

            // Game Ready Gate
            builder.Register<IGameplayReadyGate, GameplayReadyGate>(Lifetime.Singleton);

            // HUD
            builder.RegisterHud(_hudRoot, _hudWidgetRegistry, this.GetCancellationTokenOnDestroy());

            // ResourceManager
            builder.RegisterInstance(new WebClientOptions
            {
                BaseUrl = ApiConfig.BaseUrl,
                TimeoutSeconds = 30,
                DefaultHeaders = new Dictionary<string, string>()
            });
            builder.Register<IAuthTokenProvider, NoOpAuthTokenProvider>(Lifetime.Singleton);
            builder.Register<IWebClient, UnityWebRequestWebClient>(Lifetime.Singleton);

            builder.Register<IResourceAdjustApi, UnityWebRequestResourceAdjustApi>(Lifetime.Singleton);
            builder.Register<IResourceOperationsService, ResourceOperationsService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ResourceManager>().As<ResourceManager>();
            
            builder.RegisterComponentInHierarchy<AnimateCurrency>();
            builder.RegisterInstance<IGlobalTimerService>(_globalTimerService);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            builder.RegisterInstance(rewardSpecsConfig);
            builder.Register<IInventoryItemCategoryResolver>(_ => new RewardSpecInventoryItemCategoryResolver(rewardSpecsConfig), Lifetime.Singleton);
            
            builder.RegisterInventoryService();
            builder.RegisterCardCollectionImpl();
            builder.RegisterBattlePass();
            builder.RegisterFortuneWheel();
            
            // Rewards
            builder.Register<ResourceRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<InventoryRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<ResourcePlayerStateSnapshotHandler>(Lifetime.Singleton).As<IPlayerStateSnapshotHandler>();
            builder.Register<InventoryPlayerStateSnapshotHandler>(Lifetime.Singleton).As<IPlayerStateSnapshotHandler>();
            builder.Register<IPlayerStateSnapshotApplier, PlayerStateSnapshotApplier>(Lifetime.Singleton);
            builder.Register<IRewardResponseApplier, RewardResponseApplier>(Lifetime.Singleton);
            builder.Register<IRewardGrantService, ServerRewardGrantService>(Lifetime.Singleton);
            builder.Register<IRewardIntentService, ServerRewardIntentService>(Lifetime.Singleton);
            builder.Register<IRewardPlayerStateSyncService, ServerRewardPlayerStateSyncService>(Lifetime.Singleton);
            builder.Register<IRewardPlayerStateRefreshCoordinator, RewardPlayerStateRefreshCoordinator>(Lifetime.Singleton);
            builder.Register<IRewardSpecProvider>(_ => new RewardSpecProvider(rewardSpecsConfig), Lifetime.Singleton);

            builder.RegisterInstance(_rewardedAdsConfigSo);
            builder.Register<IRewardedAdsProvider>(resolver =>
            {
                var config = resolver.Resolve<RewardedAdsConfigSO>().GetOrCreate();
                var playerIdentityProvider = resolver.Resolve<IPlayerIdentityProvider>();
                var webClient = resolver.Resolve<IWebClient>();
                return RewardedAdsProviderFactory.Create(config, playerIdentityProvider, webClient);
            }, Lifetime.Singleton);
            builder.Register<AdsRewardFlowService>(Lifetime.Singleton);
            builder.Register<RewardedAdsRewardOrchestrator>(Lifetime.Singleton);
            builder.Register<FortuneWheelAdSpinOrchestrator>(Lifetime.Singleton);
            
            // Orchestration
            var liveOpsScheduleFile = string.IsNullOrWhiteSpace(_liveOpsScheduleFile) ||
                                      string.Equals(_liveOpsScheduleFile, "card_collection_schedule.json")
                ? "liveops_schedule.json"
                : _liveOpsScheduleFile;
            builder.RegisterOrchestration(liveOpsScheduleFile, _liveOpsScheduleConfigName);
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();
            
            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<WindowFactoryDI>().SetResolver(resolver);
            });
        }
    }
}
