using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Inventory.Implementation.UI;
using UISystem;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Inventory.Implementation
{
    public class CheatInventoryButton : MonoBehaviour
    {
        [SerializeField] private Button _cheatButton;

        private CancellationToken _destroyCt;
        
        private UIManager _uiManager;
        private IInventoryService _inventoryService;
        private IInventoryReadService _inventoryReadService;
        private IInventoryItemUseService _inventoryItemUseService;
        private IItemCategoryRegistry _itemCategoryRegistry;
        private IPlayerIdentityProvider _playerIdentityProvider;
        
        [Inject]
        public void Install(
            UIManager uiManager,
            IInventoryService inventoryService,
            IInventoryReadService  inventoryReadService, 
            IInventoryItemUseService inventoryItemUseService,
            IItemCategoryRegistry  itemCategoryRegistry,
            IPlayerIdentityProvider playerIdentityProvider)
        {
            _uiManager = uiManager;
            _inventoryService = inventoryService;
            _inventoryReadService = inventoryReadService;
            _inventoryItemUseService = inventoryItemUseService;
            _itemCategoryRegistry = itemCategoryRegistry;
            _playerIdentityProvider = playerIdentityProvider;
        }
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void Start()
        {
            _cheatButton.onClick.AddListener(() => OpenCheatsPanelAsync(_destroyCt).Forget());
        }

        private async UniTask OpenCheatsPanelAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_inventoryService == null)
            {
                Debug.LogWarning("Failed to open inventory window. IInventoryService are not initialized.");
                return;
            }
            
            if (_inventoryReadService == null)
            {
                Debug.LogWarning("Failed to open inventory window. IInventoryReadService are not initialized.");
                return;
            }
            
            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                Debug.LogWarning("Failed to open inventory window. Player id is empty.");
                return;
            }

            var tabsPresenter = new InventoryTabsPresenter(ownerId, _inventoryService, _inventoryReadService, _itemCategoryRegistry);
            await tabsPresenter.InitializeAsync(ct);

            var args = new InventoryArgs(_uiManager, _inventoryItemUseService, tabsPresenter, _itemCategoryRegistry.GetAllCategories());
            _uiManager.Show<InventoryWindowController>(args);
        }
        
        private void OnDestroy() 
        { 
            _cheatButton.onClick.RemoveAllListeners();
        }
    }
}
