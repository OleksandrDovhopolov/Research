using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;

namespace CardCollectionImpl
{
    public class CardPackInventoryUseHandler : IInventoryItemUseHandler
    {
        private readonly IOpenPackFlow _openPackFlowService;
        
        public CardPackInventoryUseHandler(IOpenPackFlow openPackFlowService)
        {
            _openPackFlowService =  openPackFlowService;
        }

        public bool CanHandle(InventoryItemDelta item)
        {
            return item.CategoryId == CardsConfig.CardPack;
        }

        public async UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await _openPackFlowService.OpenPackById(item.ItemId, ct);
        }
    }
}
