using System.Threading;
using CardCollection.Core;
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
        [SerializeField] private CardCollectionEntryPoint _cardCollectionEntryPoint;
        [SerializeField] private ExchangePacksConfig _exchangePacksConfig;

        private ConfigManager _configManager;
        private CancellationToken _destroyCt;
        private IExchangePackProvider _exchangePackProvider;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _destroyCt = this.GetCancellationTokenOnDestroy();
            _exchangePackProvider = new ExchangePackProvider(_exchangePacksConfig, _cardCollectionEntryPoint.CardCollectionModule);
        }

        private void Start()
        {
            LoadAddressables().Forget();
            LoadConfig().Forget();

            WindowFactoryBase windowFactoryBase = new WindowFactoryDI(_uiManager);
            UIManagerEventHandlerBase eventHandler = new UIManagerSignalHandler();

            _uiManager.Configurate(windowFactoryBase, eventHandler);

            _button.onClick.AddListener(() => OpenCardCollectionWindow().Forget());
            _cheatButton.onClick.AddListener(OpenCheatsPanel);
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

        private async UniTask OpenCardCollectionWindow()
        {
            await _cardCollectionEntryPoint.WaitForInitializationAsync();
            
            var collectionData = await _cardCollectionEntryPoint.CardCollectionReader.Load(_destroyCt);
            var args = new CardCollectionArgs(
                _uiManager,
                _cardCollectionEntryPoint.CardCollectionModule,
                collectionData,
                _exchangePackProvider);
            _uiManager.Show<CardCollectionController>(args);
        }

        private void OpenCheatsPanel()
        {
            _researchCheatModule.OpenCheatPanel();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            _cheatButton.onClick.RemoveAllListeners();
        }
    }
}
