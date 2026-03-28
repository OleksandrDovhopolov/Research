using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;

namespace CardCollectionImpl
{
    public class CardPackInventoryUseHandler : IInventoryItemUseHandler
    {
        private readonly ICardCollectionWindowOpener _cardCollectionWindowOpener;

        public CardPackInventoryUseHandler(ICardCollectionWindowOpener cardCollectionWindowOpener)
        {
            _cardCollectionWindowOpener = cardCollectionWindowOpener ?? throw new ArgumentNullException(nameof(cardCollectionWindowOpener));
        }
        
        //TODO better to rely on category type / enum ?? 
        public bool CanHandle(InventoryItemDelta item)
        {
            return item.CategoryId == CardsConfig.CardPack;
        }

        public async UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct)
        {
            _cardCollectionWindowOpener.OpenNewCardWindow(item.ItemId);
            await UniTask.CompletedTask;
        }
    }
}
