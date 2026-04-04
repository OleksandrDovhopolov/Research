using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardPackInventoryUseHandler : IInventoryItemUseHandler
    {
        private readonly ICardCollectionModule _collectionModule;
        private readonly INewCardFlowService _newCardFlowService;
        private readonly ICardCollectionWindowCoordinator _cardCollectionWindowCoordinator;

        public CardPackInventoryUseHandler(
            ICardCollectionModule collectionModule,
            INewCardFlowService newCardFlowService,
            ICardCollectionWindowCoordinator cardCollectionWindowCoordinator)
        {
            _collectionModule = collectionModule;
            _newCardFlowService = newCardFlowService;
            _cardCollectionWindowCoordinator = cardCollectionWindowCoordinator;
        }
        
        //TODO better to rely on category type / enum ?? 
        public bool CanHandle(InventoryItemDelta item)
        {
            return item.CategoryId == CardsConfig.CardPack;
        }

        public UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var packId = item.ItemId;
            var pack = _collectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return UniTask.CompletedTask;
            }

            var args = new NewCardArgs(packId, _newCardFlowService);
            _cardCollectionWindowCoordinator.ShowNewCard(args);
            return UniTask.CompletedTask;
        }
    }
}
