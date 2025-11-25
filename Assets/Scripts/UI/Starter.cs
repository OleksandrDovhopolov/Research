using System;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class Starter : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private ResearchCheatModule _researchCheatModule;
        [SerializeField] private Button _button;
        [SerializeField] private Button _cheatButton;

        private CollectionService _collectionService;
        private CollectionService CollectionService => _collectionService ??= new CollectionService();
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            WindowFactoryBase windowFactoryBase = new WindowFactoryDI(_uiManager);
            UIManagerEventHandlerBase eventHandler = new UIManagerSignalHandler();
            
            _uiManager.Configurate(windowFactoryBase, eventHandler);
            
            _button.onClick.AddListener(OpenSettings);
            _cheatButton.onClick.AddListener(OpenCheatsPanel);
        }

        private void OpenSettings()
        {
            var cardsData = CollectionService.Cards;
            var args = new CardCollectionArgs(_uiManager, cardsData);
            _uiManager.Show<CardCollectionController>(args);
        }

        private void OpenCheatsPanel()
        {
            CollectionService.PrintData();
            //_researchCheatModule.OpenCheatPanel();
        }
        
        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            _cheatButton.onClick.RemoveAllListeners();
        }
    } 
}

