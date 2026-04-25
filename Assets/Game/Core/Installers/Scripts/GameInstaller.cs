using CardCollectionImpl;
using core;
using CoreResources;
using EventOrchestration;
using BattlePass;
using FortuneWheel;
using Infrastructure;
using Inventory.API;
using Rewards;
using System.Collections.Generic;
using System.Linq;
using UIShared;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private GlobalTimerService _globalTimerService;
        [SerializeField] private RewardSpecsConfigSO _rewardSpecsConfigSo;
        [SerializeField] private RewardedAdsConfigSO _rewardedAdsConfigSo;
        [SerializeField] private string _liveOpsScheduleFile = "liveops_schedule.json";
        [SerializeField] private string _liveOpsScheduleConfigName = "cards_event_schedule";
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            if (_globalTimerService == null)
            {
                throw new MissingReferenceException($"{nameof(GlobalTimerService)} is not assigned on {nameof(GameInstaller)}.");
            }
            
            var rewardSpecsConfig = _rewardSpecsConfigSo != null
                ? _rewardSpecsConfigSo
                : ScriptableObject.CreateInstance<RewardSpecsConfigSO>();

            // Game Ready Gate
            builder.Register<IGameplayReadyGate, GameplayReadyGate>(Lifetime.Singleton);
            
            // ResourceManager
            builder.RegisterInstance(new WebClientOptions
            {
                BaseUrl = Infrastructure.ApiConfig.BaseUrl,
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
            builder.Register<IRewardSpecProvider>(_ => new RewardSpecProvider(rewardSpecsConfig), Lifetime.Singleton);

            var rewardedAdsConfig = _rewardedAdsConfigSo != null
                ? _rewardedAdsConfigSo
                : ScriptableObject.CreateInstance<RewardedAdsConfigSO>();
            builder.RegisterInstance(rewardedAdsConfig);
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

                var rewardedAdPresenters = Object.FindObjectsOfType<RewardedAdButtonPresenter>(true);
                foreach (var rewardedAdPresenter in rewardedAdPresenters)
                {
                    if (rewardedAdPresenter != null)
                    {
                        resolver.Inject(rewardedAdPresenter);
                    }
                }

                var battlePassOpenButtons = Object.FindObjectsOfType<BattlePassOpenButton>(true);
                foreach (var battlePassOpenButton in battlePassOpenButtons)
                {
                    if (battlePassOpenButton != null)
                    {
                        resolver.Inject(battlePassOpenButton);
                    }
                }
            });
        }
    }
}
