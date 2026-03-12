using System;
using System.Collections.Generic;
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
        [SerializeField] private List<ItemCategory> _categories = new();

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

            if (_categories.Count == 0)
            {
                Debug.LogWarning($"Failed to open inventory window. List<ItemCategory> is empty");
                return;
            }
            
            var tabsPresenter = new InventoryTabsPresenter(_inventoryService, _ownerId, _categories);
            await tabsPresenter.InitializeAsync(ct);

            var args = new InventoryArgs(_uiManager, tabsPresenter, _categories);
            _uiManager.Show<InventoryWindowController>(args);
        }
        
        private void OnDestroy() 
        { 
            _cheatButton.onClick.RemoveAllListeners();
        }
    }
}
