using cheatModule;
using Inventory.API;

namespace Game.Cheat
{
    public class InventoryCheatModule : ICheatsModule
    {
        private const string InventoryGroup = "InventoryGroup";
        
        public const string Regular = "regular";
        public const string CardPack = "card_pack";
        
        private readonly IInventoryService _inventoryService;
        
        public InventoryCheatModule(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gold", amount =>
            {
                const string ownerId = "player_1";
                const string itemId = "Gold";
                const string categoryId = Regular;
                
                var inventoryItemDelta = new InventoryItemDelta(ownerId, itemId, amount, categoryId);
                _inventoryService.AddItemAsync(inventoryItemDelta);
            }).WithGroup(InventoryGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gold", amount =>
            {
                const string ownerId = "player_1";
                const string itemId = "Gold";
                const string categoryId = Regular;
                
                var inventoryItemDelta = new InventoryItemDelta(ownerId, itemId, amount, categoryId);
                _inventoryService.RemoveItemAsync(inventoryItemDelta);
            }).WithGroup(InventoryGroup));
        }
    }
}