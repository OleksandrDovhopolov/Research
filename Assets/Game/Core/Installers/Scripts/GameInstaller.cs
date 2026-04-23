using CardCollectionImpl;
using core;
using CoreResources;
using EventOrchestration;
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
        [SerializeField] private string _cardCollectionScheduleFile = "card_collection_schedule.json";
        [SerializeField] private string _removeCardCollectionConfigSchedule = "cards_event_schedule";
        
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
            builder.RegisterFortuneWheel();
            
            // Rewards
            builder.Register<ResourceRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<InventoryRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<GameRewardGrantService>(resolver =>
            {
                var handlers = resolver.Resolve<IEnumerable<IRewardHandler>>().ToList();
                var rewardSpecProvider = resolver.Resolve<IRewardSpecProvider>();
                return new GameRewardGrantService(handlers, rewardSpecProvider);
            }, Lifetime.Singleton);
            builder.Register<ResourcePlayerStateSnapshotHandler>(Lifetime.Singleton).As<IPlayerStateSnapshotHandler>();
            builder.Register<InventoryPlayerStateSnapshotHandler>(Lifetime.Singleton).As<IPlayerStateSnapshotHandler>();
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
            
            // Orchestration
            builder.RegisterOrchestration(_cardCollectionScheduleFile, _removeCardCollectionConfigSchedule);
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
            });
        }
    }
}
