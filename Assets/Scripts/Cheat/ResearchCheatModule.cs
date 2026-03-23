using System;
using System.Collections.Generic;
using CardCollectionImpl;
using cheatModule;
using CoreResources;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UnityEngine;
using VContainer;

namespace core
{
    public class ResearchCheatModule : MonoBehaviour, ICheatsContainer
    {
        [SerializeField] private CheatsManager _cheatsManagerPrefab;
        [SerializeField] private ResourcesView _resourcesView;
        
        private CheatsManager _cheatsManager;
        private CheatPanelItem _rootPanel;
        private List<ICheatsModule> _cheatsModules;
        
        private IInventoryService _inventoryService;
        private ICardCollectionFeatureFacade _cardCollectionFeatureFacade;

        [Inject]
        private void Construct(IInventoryService inventoryService, ICardCollectionFeatureFacade cardCollectionFeatureFacade)
        {
            _inventoryService = inventoryService;
            _cardCollectionFeatureFacade = cardCollectionFeatureFacade;
        }
        
        public void Start()
        {
            InitializeRootPanel();
            InitializeCheatsModules();
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
        
        //TODO restore cheat when module integration is ready
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
                new CardCollectionModule(_cardCollectionFeatureFacade, destroyCt),
                new SampleModule(_resourcesView.ResourceManager),
                new InventoryModule(_inventoryService),
            };
            
            return cheatsModules;
        }
        
        public void OpenCheatPanel()
        {
            if (_cheatsManager == null)
            {
                throw new NullReferenceException("_cheatsManager");
            }
            
            _cheatsManager.ShowPanel(_rootPanel);
        }
    }
}