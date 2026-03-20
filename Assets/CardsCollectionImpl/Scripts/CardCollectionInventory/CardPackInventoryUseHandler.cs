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
            OpenNewCardWindow(item.ItemId, _cardCollectionModule, _cardCollectionReader);
            await UniTask.CompletedTask;
        }
        
        public void OpenNewCardWindow(string packId, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader)
        {
            var pack = cardCollectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return;
            }
            
            OpenNewCardWindow(pack, cardCollectionModule, cardCollectionReader);
        }

        //TODO should return true / false ? 
        public void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader)
        {
            //TODO UIStack
            // private IEnumerator ShowCommand(UIShowCommand command) 
            // if ShowInParallel  = hide and show start the same time 
            // in this case should be ShowInOrder 
            var args = new NewCardArgs(pack, cardCollectionModule, cardCollectionReader); 
            _uiManager.Show<NewCardController>(args);
        }
    }
}
