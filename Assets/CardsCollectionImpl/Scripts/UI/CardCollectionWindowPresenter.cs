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
        public void OpenNewCardWindow(string packId, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader, bool showAsync = false)
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
        public void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader, bool showAsync = false)
        {
            //TODO UIStack
            // private IEnumerator ShowCommand(UIShowCommand command) 
            // if ShowInParallel  = hide and show start the same time 
            // in this case should be ShowInOrder 
            var args = new NewCardArgs(pack, _uiManager, cardCollectionModule, cardCollectionReader); 
            _uiManager.Show<NewCardController>(args);
        }

        //TODO should return true / false ? 
        public async UniTask OpenCardCollectionWindow(
            ICardCollectionModule  cardCollectionModule,
            EventCardsSaveData  eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory  rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct)
        {
            var newCardsData = CardCollectionNewCardsDto.Create(eventCardsSaveData);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                await cardCollectionModule.ResetNewFlagsAsync(newCardIds, ct);
            }

            _collectionProgressSnapshotService.TryGetSnapshot(out var snapshot);
            var args = new CardCollectionArgs(
                _uiManager,
                newCardsData,
                eventCardsSaveData,
                exchangeOfferProvider,
                rewardDefinitionFactory, 
                cardCollectionPointsAccount,
                snapshot);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(eventCardsSaveData);
        }
    }
}