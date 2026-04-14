using System;
using System.Collections.Generic;
using CardCollectionImpl;
using cheatModule;
using CoreResources;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using Inventory.API;
using UIShared;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Cheat
{
    public class CheatModuleView : MonoBehaviour, ICheatsContainer
    {
        [SerializeField] private CheatsManager _cheatsManagerPrefab;
        [SerializeField] private Button _cheatButton; 
        
        private CheatsManager _cheatsManager;
        private CheatPanelItem _rootPanel;
        private List<ICheatsModule> _cheatsModules;
        
        private ResourceManager _resourceManager;
        private IInventoryService _inventoryService;
        private OrchestratorRunner _orchestratorRunner;
        private AnimateCurrency _animateCurrency;
        private ICardCollectionSessionFacade _cardCollectionSessionFacade;

        [Inject]
        private void Construct(
            AnimateCurrency animateCurrency,
            ResourceManager resourceManager,
            IInventoryService inventoryService, 
            OrchestratorRunner orchestratorRunner,
            ICardCollectionSessionFacade cardCollectionSessionFacade)
        {
            _inventoryService = inventoryService;
            _resourceManager = resourceManager;
            _animateCurrency = animateCurrency;
            _orchestratorRunner = orchestratorRunner;
            _cardCollectionSessionFacade = cardCollectionSessionFacade;
        }
        
        public void Start()
        {
            InitializeRootPanel();
            InitializeCheatsModules();
        }
        
        private void OnEnable()
        {
            _cheatButton.onClick.AddListener(OpenCheatPanel);
        }

        private void OnDisable()
        {
            _cheatButton.onClick.RemoveListener(OpenCheatPanel);
        }
        
        private void InitializeRootPanel()
        {
            if (_cheatsManager == null)
            {
                _cheatsManager = Instantiate(_cheatsManagerPrefab);
            }

            if (_cheatsManager == null)
            {
                throw new NullReferenceException("_cheatsManager");
            }
            
            _rootPanel = _cheatsManager.GetCheatItem<CheatPanelItem>();
            _rootPanel.transform.SetParent(_cheatsManager.transform, false);
        }
        
        private void InitializeCheatsModules()
        {
            _cheatsModules = new List<ICheatsModule>(GetCheatModules());
            _cheatsModules.ForEach(module =>
            {
                module.Initialize(this);
            });
        }
        
        public void AddItem<T>(Action<T> initializer) where T : CheatItem
        {
            _rootPanel.AddItem(initializer);
        }
        
        protected virtual List<ICheatsModule> GetCheatModules()
        {
            var destroyCt = this.GetCancellationTokenOnDestroy();
            
            var cheatsModules = new List<ICheatsModule>
            {
                new CardCollectionCheatModule(_cardCollectionSessionFacade, _orchestratorRunner, destroyCt),
                new AddCardsCheatModule(_cardCollectionSessionFacade, destroyCt),
                new ResourcesCheatModule(_resourceManager, _animateCurrency),
                //new InventoryCheatModule(_inventoryService),
            };
            
            return cheatsModules;
        }
        
        public void OpenCheatPanel()
        {
            if (_cheatsManager == null)
            {
                Debug.LogWarning("Failed to open inventory window. Inventory services are not initialized.");
                throw new NullReferenceException("_cheatsManager");
            }
            
            _cheatsManager.ShowPanel(_rootPanel);
        }
    }
}