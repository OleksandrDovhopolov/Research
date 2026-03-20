using Resources.Core;
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
            builder.RegisterOrchestration(_cardCollectionScheduleFile);

            builder.RegisterComponentInHierarchy<Starter>();
            builder.RegisterComponentInHierarchy<OrchestratorRunner>();
        }
    }
}
