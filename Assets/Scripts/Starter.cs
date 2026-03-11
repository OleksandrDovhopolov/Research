using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Resources.Core;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace core
{
    public class Starter : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private ResearchCheatModule _researchCheatModule;
        [SerializeField] private Button _button;
        [SerializeField] private Button _cheatButton;
        [SerializeField] private CardCollectionEntryPoint _cardCollectionEntryPoint;
        [SerializeField] private InventoryEntryPoint _inventoryEntryPoint;
        [SerializeField] private ScriptableObject _exchangePacksConfig;
        [SerializeField] private string _inventoryOwnerId = "player_1";
        
        [Space, Header("Resources")]
        [SerializeField] private ResourcesView _resourcesView;

        private ConfigManager _configManager;
        private CancellationToken _destroyCt;
        
        private ResourceManager _resourceManager;
        private ICardPackProvider _cardPackProvider;
        private IWindowPresenter _windowPresenter;
        private ICardCollectionRewardHandler _rewardHandler;
        private IExchangeOfferProvider _exchangeOfferProvider;
        private IRewardDefinitionFactory _rewardDefinitionFactory;
        private ICardCollectionCompositionRoot _compositionRoot;

        [Inject]
        private void Construct(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            WindowFactoryBase windowFactoryBase = new WindowFactoryDI(_uiManager);
            UIManagerEventHandlerBase eventHandler = new UIManagerSignalHandler();

            _uiManager.Configurate(windowFactoryBase, eventHandler);

            _button.onClick.AddListener(() => OpenCardCollectionWindow().Forget());
            _cheatButton.onClick.AddListener(OpenCheatsPanel);
            
            Init(_destroyCt).Forget();
        }

        private async UniTask Init(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _compositionRoot = CardCollectionCompositionRegistry.Resolve();
            _cardPackProvider = new JsonCardPackProvider();
            
            await LoadAddressables(ct);
            await LoadConfig(ct); 
            await InitResources(ct);
            
            await InitializeRewardHandlerAsync(ct);
            await _cardCollectionEntryPoint.InitCardCollection(_cardPackProvider, _rewardHandler, ct);
            
            
            //TODO. should be called on start in order to create snapshot in CardCollectionWindowPresenter
            // find way to make it clear
            //TODO + bake sprites for groups to prevent loading when window open
            var collectionData = await _cardCollectionEntryPoint.CardCollectionReader.Load(_destroyCt);
            _windowPresenter = _compositionRoot.CreateWindowPresenter(collectionData);
        }

        private async UniTask LoadAddressables(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await AddressablesUpdater.CheckAndUpdateAsync();
            ct.ThrowIfCancellationRequested();
        }

        private async UniTask LoadConfig(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configManager != null) return;

            _configManager = new ConfigManager();
            var configStorages = _configManager.GetAllConfigStorages();
            foreach (var configStorage in configStorages)
            {
                configStorage.Configurate(_configManager);
            }

            var groudId = "cardGroups";
            var configFile = _configManager.GetConfigFile(groudId);
            if (configFile == null)
            {
                Debug.LogWarning($"Failed to find configFile for groupId {groudId}");
                return;
            }
            //_configManager.GetConfigFile(groudId).CurLoader = ConfigManager.LocalLoader;
            configFile.CurLoader = ConfigManager.ResourcesLoader;

            await _configManager.ApplyParsedConfigs(configStorages);
            ct.ThrowIfCancellationRequested();
        }

        
        private readonly UniTaskCompletionSource _rewardHandlerInitializationSource = new();
        public async UniTask InitializeRewardHandlerAsync(CancellationToken ct = default)
        {
            try
            {
                var cardPackConfigs = await _cardPackProvider.GetCardConfigsAsync(ct);
                _rewardDefinitionFactory = _compositionRoot.CreateRewardDefinitionFactory(cardPackConfigs);
                IInventoryService inventoryService = _inventoryEntryPoint != null ? _inventoryEntryPoint.InventoryService : null;
                var rewardGrantService = new GameRewardGrantService(_resourceManager, inventoryService, _inventoryOwnerId);
                
                _rewardHandler = _compositionRoot.CreateRewardHandler(rewardGrantService, _rewardDefinitionFactory);
                await _rewardHandler.InitializeAsync(ct);
                _rewardHandlerInitializationSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                _rewardHandlerInitializationSource.TrySetCanceled(ct);
                throw;
            }
            catch (Exception ex)
            {
                _rewardHandlerInitializationSource.TrySetException(ex);
                throw;
            }
        }
        
        
        public UniTask WaitForRewardHandlerInitializationAsync(CancellationToken ct = default)
        {
            return _rewardHandlerInitializationSource.Task.AttachExternalCancellation(ct);
        }
        
        private async UniTask OpenCardCollectionWindow()
        {
            await _cardCollectionEntryPoint.WaitForInitializationAsync();
            await WaitForRewardHandlerInitializationAsync(_destroyCt);

            _exchangeOfferProvider ??= _compositionRoot.CreateExchangeOfferProvider(_rewardHandler);
            
            var collectionData = await _cardCollectionEntryPoint.CardCollectionReader.Load(_destroyCt);
            
            await _windowPresenter.OpenCardCollectionWindow( 
                _cardCollectionEntryPoint.CardCollectionModule,
                collectionData,
                _exchangeOfferProvider,
                _rewardDefinitionFactory,
                _cardCollectionEntryPoint.CardCollectionPointsAccount,
                _destroyCt);
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
            _resourceManager?.Dispose();
        }
    }
}
