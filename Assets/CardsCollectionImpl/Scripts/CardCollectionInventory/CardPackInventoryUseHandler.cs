using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardPackInventoryUseHandler : IInventoryItemUseHandler
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionModule _cardCollectionModule;
        private readonly ICardCollectionReader _cardCollectionReader;

        public CardPackInventoryUseHandler(
            UIManager uiManager, 
            ICardCollectionModule cardCollectionModule,
            ICardCollectionReader cardCollectionReader)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
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
            OpenNewCardWindow(item.ItemId);
            await UniTask.CompletedTask;
        }
        
        public void OpenNewCardWindow(string packId)
        {
            var pack = _cardCollectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return;
            }
            
            OpenNewCardWindow(pack);
        }

        //TODO should return true / false ? 
        public void OpenNewCardWindow(CardPack pack)
        {
            var args = new NewCardArgs(pack, _cardCollectionModule, _cardCollectionReader); 
            _uiManager.Show<NewCardController>(args);
        }
    }
}
