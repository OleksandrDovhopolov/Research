using System;
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
        
        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
            _inventoryService = InventoryCompositionRegistry.Resolve().CreateInventoryService();
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
                Debug.LogWarning($"Failed to open inventory window. IInventoryService is null");
                return;
            }

            var tabsPresenter = new InventoryTabsPresenter(_inventoryService, _ownerId);
            await tabsPresenter.InitializeAsync(ct);

            var args = new InventoryArgs(_uiManager, tabsPresenter);
            _uiManager.Show<InventoryWindowController>(args);
        }
        
        private void OnDestroy() 
        { 
            _cheatButton.onClick.RemoveAllListeners();
        }
    }
}
