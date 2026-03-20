using System.Threading;
using CardCollection.Core;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using Infrastructure;
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
        [SerializeField] private Button _cheatButton;
        [SerializeField] private InventoryEntryPoint _inventoryEntryPoint;
        [SerializeField] private string _inventoryOwnerId = "player_1";
        
        [Space, Header("Resources")]
        [SerializeField] private ResourcesView _resourcesView;

        private ConfigManager _configManager;
        private CancellationToken _destroyCt;
        
        private IObjectResolver _resolver;
        private ResourceManager _resourceManager;
        
        [Inject]
        private void Construct(ResourceManager resourceManager, IObjectResolver resolver)
        {
            _resourceManager = resourceManager;
            _resolver = resolver;
        }
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            WindowFactoryBase windowFactoryBase = new WindowFactoryDI(_uiManager, _resolver);
            UIManagerEventHandlerBase eventHandler = new UIManagerSignalHandler();

            _uiManager.Configurate(windowFactoryBase, eventHandler);

            _cheatButton.onClick.AddListener(OpenCheatsPanel);
            
            Init(_destroyCt).Forget();
        }

        private DebugRewardCreator _debugRewardCreator;
        
        private async UniTask Init(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            await LoadAddressables(ct);
            await LoadConfig(ct); 
            await InitResources(ct);
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
            _cheatButton.onClick.RemoveAllListeners();
            _resourceManager?.Dispose();
        }
    }
}
