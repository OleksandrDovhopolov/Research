using CardCollectionImpl;
using core;
using CoreResources;
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
        [SerializeField] private ExchangePacksConfig _exchangePacksConfig;
        [SerializeField] private string _cardCollectionScheduleFile = "card_collection_schedule.json";

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
            
            if (_exchangePacksConfig == null)
            {
                throw new MissingReferenceException($"{nameof(ExchangePacksConfig)} is not assigned on {nameof(GameInstaller)}.");
            }

            builder.RegisterInstance(_uiManager);
            builder.RegisterInstance<IHUDService>(_hudService);
            builder.Register<JsonResourcesStorage>(Lifetime.Singleton);
            builder.Register<ResourceManager>(Lifetime.Singleton);
            builder.RegisterInventoryService();
            builder.RegisterCardCollectionImpl(_exchangePacksConfig);
            
            builder.RegisterComponentInHierarchy<AnimateCurrency>();
            
            // Rewards.asmdef
            builder.Register<IRewardGrantService>(resolver =>
            {
                var resourceManager = resolver.Resolve<ResourceManager>();
                var inventoryService = resolver.Resolve<IInventoryService>();
                var animateCurrency = resolver.Resolve<AnimateCurrency>();
                var inventoryOwnerId = resolver.Resolve<string>();
                return new GameRewardGrantService(animateCurrency, resourceManager, inventoryService, inventoryOwnerId);
            }, Lifetime.Singleton);

            builder.RegisterOrchestration(_cardCollectionScheduleFile);

            builder.RegisterComponentInHierarchy<Bootstrap>();
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();
        }
    }
}
