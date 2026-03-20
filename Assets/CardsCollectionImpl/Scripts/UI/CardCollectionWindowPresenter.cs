using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionWindowPresenter : IWindowPresenter
    {
        private readonly UIManager _uiManager;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;
        
        public CardCollectionWindowPresenter(
            UIManager uiManager, 
            ICollectionProgressSnapshotService collectionProgressSnapshotService,
            EventCardsSaveData eventCardsSaveData = null)
        {
            _uiManager = uiManager;
            _collectionProgressSnapshotService = collectionProgressSnapshotService;

            if (eventCardsSaveData != null)
            {
                _collectionProgressSnapshotService.SetSnapshot(eventCardsSaveData);
            }
        }

        //TODO should return true / false ? 
        public async UniTask OpenCardCollectionWindow(
            ICardCollectionModule  cardCollectionModule,
            ICardCollectionReader  cardCollectionReader,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory  rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct)
        {
            var collectionData = await cardCollectionReader.Load(ct);
            
            var newCardsData = CardCollectionNewCardsDto.Create(collectionData);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                //TODO check do i need here await
                await cardCollectionModule.ResetNewFlagsAsync(newCardIds, ct);
            }

            _collectionProgressSnapshotService.TryGetSnapshot(out var snapshot);
            var args = new CardCollectionArgs(
                newCardsData,
                collectionData,
                exchangeOfferProvider,
                rewardDefinitionFactory, 
                cardCollectionPointsAccount,
                snapshot);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(collectionData);
        }
    }
}