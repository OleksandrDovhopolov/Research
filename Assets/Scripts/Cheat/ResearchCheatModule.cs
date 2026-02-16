using System;
using System.Collections.Generic;
using cheatModule;
using UnityEngine;

namespace core
{
    public class ResearchCheatModule : MonoBehaviour, ICheatsContainer
    {
        [SerializeField] private CheatsManager _cheatsManagerPrefab;
        [SerializeField] private CardCollectionEntryPoint _cardCollectionEntryPoint;
        private CheatsManager _cheatsManager;
        
        private CheatPanelItem _rootPanel;
        private List<ICheatsModule> _cheatsModules;
        
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
            var updated = _cardCollectionEntryPoint.CardCollectionUpdater;
            var reader = _cardCollectionEntryPoint.CardCollectionReader;
            
            var cheatsModules = new List<ICheatsModule>
            {
                new DefaultModule(updated, reader),
                new SampleModule(),
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