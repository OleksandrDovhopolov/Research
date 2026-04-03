using CardCollectionImpl;
using core;
using CoreResources;
using EventOrchestration;
using Infrastructure.SaveSystem;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public sealed class GameInstaller : LifetimeScope
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private HUDService _hudService;
        [SerializeField] private GlobalTimerService _globalTimerService;
        [SerializeField] private RewardSpecsConfigSO _rewardSpecsConfigSo;
        [SerializeField] private string _cardCollectionScheduleFile = "card_collection_schedule.json";
        [SerializeField] private string _removeCardCollectionConfigSchedule = "cards_event_schedule";
        
        protected override void Configure(IContainerBuilder builder)
        {
            if (_uiManager == null)
            {
                throw new MissingReferenceException($"{nameof(UIManager)} is not assigned on {nameof(GameInstaller)}.");
            }
            
            if (_hudService == null)
            {
                throw new MissingReferenceException($"{nameof(HUDService)} is not assigned on {nameof(GameInstaller)}.");
            }

            if (_globalTimerService == null)
            {
                throw new MissingReferenceException($"{nameof(GlobalTimerService)} is not assigned on {nameof(GameInstaller)}.");
            }

            builder.RegisterInstance(_uiManager);
            builder.RegisterInstance<IHUDService>(_hudService);
            builder.RegisterInstance<IGlobalTimerService>(_globalTimerService);
            builder.Register<SaveMigrationService>(Lifetime.Singleton);
            builder.Register<ISaveStorage>(_ => new LocalDiskStorage("global_save.json"), Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton);
            builder.Register<ResourceManager>(Lifetime.Singleton);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            
            builder.RegisterInventoryService();
            builder.RegisterCardCollectionImpl();
            
            builder.RegisterComponentInHierarchy<AnimateCurrency>();
            
            // Rewards.asmdef
            builder.Register<IRewardGrantService>(resolver =>
            {
                var resourceManager = resolver.Resolve<ResourceManager>();
                var inventoryService = resolver.Resolve<IInventoryService>();
                var inventoryOwnerId = resolver.Resolve<string>();
                return new GameRewardGrantService(resourceManager, inventoryService, inventoryOwnerId);
            }, Lifetime.Singleton);

            builder.Register<IRewardSpecProvider>(_ => new RewardSpecProvider(_rewardSpecsConfigSo), Lifetime.Singleton); 
            
            // Orchestration.asmdef
            builder.RegisterOrchestration(_cardCollectionScheduleFile, _removeCardCollectionConfigSchedule);
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();

            builder.RegisterComponentInHierarchy<Bootstrap>();
        }
    }
}
