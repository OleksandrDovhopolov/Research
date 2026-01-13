using Cysharp.Threading.Tasks;
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
        [SerializeField] private Button _newCardButton;
        [SerializeField] private CardCollectionSaveController _cardCollectionSaveController;
        
        private ConfigManager _configManager;
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            LoadAddressables().Forget();
            LoadConfig().Forget();
            
            WindowFactoryBase windowFactoryBase = new WindowFactoryDI(_uiManager);
            UIManagerEventHandlerBase eventHandler = new UIManagerSignalHandler();
            
            _uiManager.Configurate(windowFactoryBase, eventHandler);
            
            _button.onClick.AddListener(() => OpenSettings().Forget());
            _cheatButton.onClick.AddListener(OpenCheatsPanel);
            _newCardButton.onClick.AddListener(OpenNewCardWindow);
        }

         private async UniTask LoadAddressables()
         {
             await AddressablesUpdater.CheckAndUpdateAsync();
         }
         
        private async UniTask LoadConfig()
        {
            if (_configManager != null) return;
            
            _configManager = new ConfigManager();
            var configStorages = _configManager.GetAllConfigStorages();
            foreach (var configStorage in configStorages)
            {
                configStorage.Configurate(_configManager);
            }
            
            var groudId = "cardGroups";
            //_configManager.GetConfigFile(groudId).CurLoader = ConfigManager.LocalLoader;
            _configManager.GetConfigFile(groudId).CurLoader = ConfigManager.ResourcesLoader;
            
            
            await _configManager.ApplyParsedConfigs(configStorages);
        }
        
        private async UniTask OpenSettings()
        {
            EventCardsSaveData collectionData = await _cardCollectionSaveController.Load();
            var args = new CardCollectionArgs(_uiManager, _cardCollectionSaveController, collectionData);
            _uiManager.Show<CardCollectionController>(args);
        }

        private void OpenCheatsPanel()
        {
            _researchCheatModule.OpenCheatPanel();
        }

        public void OpenNewCardWindow()
        {
            var cardRandomizer = new SimpleNewCardsRandomizer();
            var args = new NewCardArgs(_uiManager, cardRandomizer, _cardCollectionSaveController);
            _uiManager.Show<NewCardController>(args);
        }
        
        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            _cheatButton.onClick.RemoveAllListeners();
            _newCardButton.onClick.RemoveAllListeners();
        }
    } 
}

