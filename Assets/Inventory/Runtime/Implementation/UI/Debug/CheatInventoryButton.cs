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
            var items = await LoadItemsAsync(ct);
            var args = new InventoryArgs(_uiManager, items);
            _uiManager.Show<InventoryWindowController>(args);
        }

        private async UniTask<IReadOnlyList<InventoryItemUiModel>> LoadItemsAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_inventoryService == null)
            {
                return Array.Empty<InventoryItemUiModel>();
            }

            var regularItems = await _inventoryService.GetItemsAsync(_ownerId, InventoryItemCategory.Regular, ct);
            var cardPacks = await _inventoryService.GetItemsAsync(_ownerId, InventoryItemCategory.CardPack, ct);

            var mapped = new List<InventoryItemUiModel>(regularItems.Count + cardPacks.Count);
            foreach (var item in regularItems)
            {
                mapped.Add(new InventoryItemUiModel(item.ItemId, item.ItemType, item.StackCount));
            }

            foreach (var item in cardPacks)
            {
                var subtitle = item.CardPackMetadata.HasValue
                    ? $"{item.CardPackMetadata.Value.CardsInside} cards"
                    : string.Empty;

                var title = item.CardPackMetadata.HasValue
                    ? item.CardPackMetadata.Value.PackName
                    : item.ItemType;

                mapped.Add(new InventoryItemUiModel(item.ItemId, title, item.StackCount, subtitle));
            }

            return mapped;
        }
        
        private void OnDestroy() 
        { 
            _cheatButton.onClick.RemoveAllListeners();
        }
    }
}
