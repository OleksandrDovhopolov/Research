using System;
using System.Collections.Generic;
using System.Threading;
using cheatModule;
using Cysharp.Threading.Tasks;
using Resources.Core;
using UnityEngine;

namespace core
{
    public class ResearchCheatModule : MonoBehaviour, ICheatsContainer
    {
        [SerializeField] private CheatsManager _cheatsManagerPrefab;
        [SerializeField] private InventoryEntryPoint _inventoryEntryPoint;
        [SerializeField] private ResourcesView _resourcesView;
        
        private CheatsManager _cheatsManager;
        private CheatPanelItem _rootPanel;
        private List<ICheatsModule> _cheatsModules;
        
        public void Start()
        {
            InitializeRootPanel();
            InitializeCheatsModules().Forget();
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
        private async UniTask InitializeCheatsModules()
        {
            //await _cardCollectionEntryPoint.WaitForInitializationAsync();
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
            //var updated = _cardCollectionEntryPoint.CardCollectionUpdater;
            //var reader = _cardCollectionEntryPoint.CardCollectionReader;
            //var pointsAccount = _cardCollectionEntryPoint.CardCollectionPointsAccount;
            CancellationToken destroyCt = this.GetCancellationTokenOnDestroy();
            
            var cheatsModules = new List<ICheatsModule>
            {
                //new CardCollectionModule(updated, reader, pointsAccount, destroyCt),
                new SampleModule(_resourcesView.ResourceManager),
                new InventoryModule(_inventoryEntryPoint.InventoryService),
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