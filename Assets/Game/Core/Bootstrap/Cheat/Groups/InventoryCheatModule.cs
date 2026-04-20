using cheatModule;
using Infrastructure;
using Inventory.API;
using UnityEngine;

namespace Game.Cheat
{
    public class InventoryCheatModule : ICheatsModule
    {
        private const string InventoryGroup = "InventoryGroup";
        
        public const string Regular = "regular";
        public const string CardPack = "card_pack";
        
        private readonly IInventoryService _inventoryService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        
        public InventoryCheatModule(IInventoryService inventoryService, IPlayerIdentityProvider playerIdentityProvider)
        {
            _inventoryService = inventoryService ?? throw new System.ArgumentNullException(nameof(inventoryService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new System.ArgumentNullException(nameof(playerIdentityProvider));
        }
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gold", amount =>
            {
                Debug.LogWarning("[InventoryCheatModule] Add is disabled in server-authoritative inventory mode.");
            }).WithGroup(InventoryGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gold", amount =>
            {
                var ownerId = _playerIdentityProvider.GetPlayerId();
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    Debug.LogWarning("[InventoryCheatModule] Player id is empty.");
                    return;
                }

                const string itemId = "Gold";
                const string categoryId = Regular;
                
                var inventoryItemDelta = new InventoryItemDelta(ownerId, itemId, amount, categoryId);
                _inventoryService.RemoveItemAsync(inventoryItemDelta);
            }).WithGroup(InventoryGroup));
        }
    }
}
