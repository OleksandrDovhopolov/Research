using System.Threading;
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
        
        [Space, Header("Resources")]
        [SerializeField] private ResourcesView _resourcesView;

        private ConfigManager _configManager;
        private CancellationToken _destroyCt;
        private IExchangeOfferProvider _exchangeOfferProvider;
        private IOfferRewardsReceiver _offerRewardsReceiver;
        private readonly ResourceManager _resourceManager = new();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            LoadAddressables().Forget();
            LoadConfig().Forget();
            InitResources(_destroyCt).Forget();

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

            _exchangeOfferProvider ??=
                new ExchangeOfferProvider(
                    _exchangePacksConfig,
                    _cardCollectionEntryPoint.CardCollectionPointsAccount,
                    _cardCollectionEntryPoint.CardPackProvider.GetCardConfigByIdAsync,
                    _offerRewardsReceiver ??= new OfferRewardsReceiver(_resourceManager),
                    _uiManager);
            
            var collectionData = await _cardCollectionEntryPoint.CardCollectionReader.Load(_destroyCt);
            var args = new CardCollectionArgs(
                _uiManager,
                _cardCollectionEntryPoint.CardCollectionModule,
                collectionData,
                _exchangeOfferProvider);
            _uiManager.Show<CardCollectionController>(args);
        }

        private async UniTask InitResources(CancellationToken ct)
        {
            _resourcesView.InitView(_resourceManager);
            await _resourceManager.InitializeAsync(ct);
            _resourcesView.UpdateFromResourceManager(true);
        }

        private void OpenCheatsPanel()
        {
            _researchCheatModule.OpenCheatPanel();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            _cheatButton.onClick.RemoveAllListeners();
            _resourceManager.Dispose();
        }
    }
}
