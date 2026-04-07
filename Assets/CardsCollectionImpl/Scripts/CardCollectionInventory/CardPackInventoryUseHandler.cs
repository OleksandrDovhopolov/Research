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
        private readonly IOpenPackFlowService _openPackFlowService;
        private readonly ICardCollectionWindowCoordinator _cardCollectionWindowCoordinator;

        public CardPackInventoryUseHandler(
            ICardCollectionModule collectionModule,
            IOpenPackFlowService openPackFlowService,
            ICardCollectionWindowCoordinator cardCollectionWindowCoordinator)
        {
            _collectionModule = collectionModule;
            _openPackFlowService = openPackFlowService;
            _cardCollectionWindowCoordinator = cardCollectionWindowCoordinator;
        }
        
        //TODO better to rely on category type / enum ?? 
        public bool CanHandle(InventoryItemDelta item)
        {
            return item.CategoryId == CardsConfig.CardPack;
        }

        public async UniTask UseAsync(InventoryItemDelta item, string ownerId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var packId = item.ItemId;
            var pack = _collectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return;
            }

            var screenData = await _openPackFlowService.LoadAsync(packId, ct);
            var args = new NewCardArgs(screenData);
            ct.ThrowIfCancellationRequested();
            _cardCollectionWindowCoordinator.ShowNewCard(args);
        }
    }
}
