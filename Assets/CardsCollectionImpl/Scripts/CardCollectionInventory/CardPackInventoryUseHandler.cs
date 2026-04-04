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
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionPointsAccount _collectionPointsAccount;
        private readonly ICardCollectionWindowCoordinator _cardCollectionWindowCoordinator;

        public CardPackInventoryUseHandler(
            ICardCollectionModule collectionModule,
            ICardCollectionPointsAccount collectionPointsAccount,
            ICardCollectionWindowCoordinator cardCollectionWindowCoordinator)
        {
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _collectionPointsAccount = collectionPointsAccount ?? throw new ArgumentNullException(nameof(collectionPointsAccount));
            _cardCollectionWindowCoordinator = cardCollectionWindowCoordinator ?? throw new ArgumentNullException(nameof(cardCollectionWindowCoordinator));
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

            var args = new NewCardArgs(_collectionModule.EventId, packId, _collectionModule, _collectionPointsAccount);
            _cardCollectionWindowCoordinator.ShowNewCard(args);
            return UniTask.CompletedTask;
        }
    }
}
