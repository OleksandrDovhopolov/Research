using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.UI;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Implementation
{
    public class CheatInventoryButton : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Button _cheatButton;
        [SerializeField] private string _ownerId = "player_1";

        private CancellationToken _destroyCt;
        private IInventoryService _inventoryService;
        private IInventoryReadService _inventoryReadService;
        private IInventoryItemUseService _inventoryItemUseService;
        private IItemCategoryRegistry _itemCategoryRegistry;
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
            var compositionRoot = InventoryCompositionRegistry.Resolve();
            _inventoryService = compositionRoot.CreateInventoryService();
            _inventoryReadService = compositionRoot.CreateInventoryReadService();
            _inventoryItemUseService = compositionRoot.CreateInventoryItemUseService();
            //_itemCategoryRegistry = new ItemCategoryRegistry();
            _itemCategoryRegistry = compositionRoot.GetCategoryRegistry();
        }

        private void Start()
        {
            _cheatButton.onClick.AddListener(() => OpenCheatsPanelAsync(_destroyCt).Forget());
        }

        private async UniTask OpenCheatsPanelAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_inventoryService == null || _inventoryReadService == null)
            {
                Debug.LogWarning("Failed to open inventory window. Inventory services are not initialized.");
                return;
            }
            
            var tabsPresenter = new InventoryTabsPresenter(_ownerId, _inventoryService, _inventoryReadService, _itemCategoryRegistry);
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
