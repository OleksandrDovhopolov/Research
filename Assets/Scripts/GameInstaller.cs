using CardCollection.Core;
using CardCollectionImpl;
using CoreResources;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace core
{
    public sealed class GameInstaller : LifetimeScope
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private HUDService _hudService;
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

            builder.RegisterInstance(_uiManager);
            builder.RegisterInstance<IHUDService>(_hudService);
            builder.Register<JsonResourcesStorage>(Lifetime.Singleton);
            builder.Register<ResourceManager>(Lifetime.Singleton);
            builder.RegisterInventoryService();

            //TODO this should be in CardCollectionImplInstaller. but CardCollectionController crashes because cant resolve 
            // dependencies from CardCollectionImplInstaller. Bug in WindowFactoryDI -  var controller = _diContainer.Resolve<T>();
            // Card collection feature storage
            builder.Register<ICardPackProvider, JsonCardPackProvider>(Lifetime.Singleton);
            builder.Register<ICardsConfigProvider, JsonCardsConfigProvider>(Lifetime.Singleton);
            builder.Register<ICardGroupsConfigProvider, JsonCardGroupsConfigProvider>(Lifetime.Singleton);
            // Points calculator
            builder.Register<ICardCollectionCacheService, CardCollectionCardCollectionCacheService>(Lifetime.Singleton);
            builder.Register<ICardPointsCalculator, CardsCollectionPointsCalculator>(Lifetime.Singleton);
            
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

            builder.RegisterComponentInHierarchy<Starter>();
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();
        }
    }
}
