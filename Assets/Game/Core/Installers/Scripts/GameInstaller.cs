using CardCollectionImpl;
using core;
using CoreResources;
using EventOrchestration;
using FortuneWheel;
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
        [SerializeField] private string _cardCollectionScheduleFile = "card_collection_schedule.json";
        [SerializeField] private string _removeCardCollectionConfigSchedule = "cards_event_schedule";
        
        public override void InstallBindings(IContainerBuilder builder)
        {
            if (_globalTimerService == null)
            {
                throw new MissingReferenceException($"{nameof(GlobalTimerService)} is not assigned on {nameof(GameInstaller)}.");
            }

            // Game Ready Gate
            builder.Register<IGameplayReadyGate, GameplayReadyGate>(Lifetime.Singleton);
            
            // ResourceManager
            
            builder.Register<IResourceAdjustApi, UnityWebRequestResourceAdjustApi>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ResourceManager>().As<ResourceManager>();
            
            builder.RegisterComponentInHierarchy<AnimateCurrency>();
            builder.RegisterInstance<IGlobalTimerService>(_globalTimerService);
            builder.Register<IAnimationService, AnimationService>(Lifetime.Singleton);
            
            builder.RegisterInventoryService();
            builder.RegisterCardCollectionImpl();
            builder.RegisterFortuneWheel();
            
            // Rewards
            builder.Register<ResourceRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<InventoryRewardHandler>(Lifetime.Singleton).As<IRewardHandler>();
            builder.Register<IRewardGrantService>(resolver =>
            {
                var handlers = resolver.Resolve<IEnumerable<IRewardHandler>>().ToList();
                return new GameRewardGrantService(handlers);
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
