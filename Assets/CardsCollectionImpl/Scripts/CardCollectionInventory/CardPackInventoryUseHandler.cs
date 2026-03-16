using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardPackInventoryUseHandler : IInventoryItemUseHandler
    {
        private readonly IWindowPresenter _windowPresenter;
        private readonly ICardCollectionModule _cardCollectionModule;
        private readonly ICardCollectionReader _cardCollectionReader;

        public CardPackInventoryUseHandler(
            IWindowPresenter windowPresenter,
            ICardCollectionModule cardCollectionModule,
            ICardCollectionReader cardCollectionReader)
        {
            _windowPresenter = windowPresenter ?? throw new ArgumentNullException(nameof(windowPresenter));
            _cardCollectionModule = cardCollectionModule ?? throw new ArgumentNullException(nameof(cardCollectionModule));
            _cardCollectionReader = cardCollectionReader ?? throw new ArgumentNullException(nameof(cardCollectionReader));
        }
        
        //TODO better to rely on category type / enum ?? 
        public bool CanHandle(InventoryItemDelta item)
        {
            return item.CategoryId == CardsConfig.CardPack;
        }

        public async UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct)
        {
            Debug.LogWarning($"Debug UseAsync {GetType().Name}");
            //_windowPresenter.OpenNewCardWindow(item.ItemId, _cardCollectionModule, _cardCollectionReader);
            await UniTask.CompletedTask;
        }
    }
}
