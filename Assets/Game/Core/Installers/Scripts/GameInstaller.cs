using CardCollectionImpl;
using core;
using CoreResources;
using EventOrchestration;
using Inventory.API;
using Rewards;
using UIShared;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private HUDService _hudService;
        [SerializeField] private GlobalTimerService _globalTimerService;
        [SerializeField] private RewardSpecsConfigSO _rewardSpecsConfigSo;
        [SerializeField] private string _cardCollectionScheduleFile = "card_collection_schedule.json";
        [SerializeField] private string _removeCardCollectionConfigSchedule = "cards_event_schedule";
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            if (_hudService == null)
            {
                throw new MissingReferenceException($"{nameof(HUDService)} is not assigned on {nameof(GameInstaller)}.");
            }

            if (_globalTimerService == null)
            {
                throw new MissingReferenceException($"{nameof(GlobalTimerService)} is not assigned on {nameof(GameInstaller)}.");
            }

            // Game Ready Gate
            builder.Register<IGameplayReadyGate, GameplayReadyGate>(Lifetime.Singleton);
            
            // ResourceManager
            builder.RegisterEntryPoint<ResourceManager>().As<ResourceManager>();
            
            builder.RegisterInstance<IHUDService>(_hudService);
            builder.RegisterComponentInHierarchy<AnimateCurrency>();
            builder.RegisterInstance<IGlobalTimerService>(_globalTimerService);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            
            builder.RegisterInventoryService();
            builder.RegisterCardCollectionImpl();
            
            // Rewards
            builder.Register<IRewardGrantService>(resolver =>
            {
                var resourceManager = resolver.Resolve<ResourceManager>();
                var inventoryService = resolver.Resolve<IInventoryService>();
                var inventoryOwnerId = resolver.Resolve<string>();
                return new GameRewardGrantService(resourceManager, inventoryService, inventoryOwnerId);
            }, Lifetime.Singleton);
            builder.Register<IRewardSpecProvider>(_ => new RewardSpecProvider(_rewardSpecsConfigSo), Lifetime.Singleton); 
            
            // Orchestration
            builder.RegisterOrchestration(_cardCollectionScheduleFile, _removeCardCollectionConfigSchedule);
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();
            
            builder.RegisterBuildCallback(resolver =>
            {
                resolver.Resolve<WindowFactoryDI>().SetResolver(resolver);
            });
        }
    }
}
