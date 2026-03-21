using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardPackWindowOpener : ICardPackWindowOpener
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionModule _cardCollectionModule;
        private readonly ICardCollectionReader _cardCollectionReader;
        private readonly CollectionProgressSnapshotService _collectionProgressSnapshotService;

        public CardPackWindowOpener(
            UIManager uiManager,
            ICardCollectionModule cardCollectionModule,
            ICardCollectionReader cardCollectionReader,
            CollectionProgressSnapshotService collectionProgressSnapshotService)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _cardCollectionModule = cardCollectionModule ?? throw new ArgumentNullException(nameof(cardCollectionModule));
            _cardCollectionReader = cardCollectionReader ?? throw new ArgumentNullException(nameof(cardCollectionReader));
            _collectionProgressSnapshotService = collectionProgressSnapshotService ?? throw new ArgumentNullException(nameof(collectionProgressSnapshotService));
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

        public void OpenNewCardWindow(CardPack pack)
        {
            var args = new NewCardArgs(pack, _cardCollectionModule, _cardCollectionReader);
            _uiManager.Show<NewCardController>(args);
        }
        
        public async UniTask OpenCardCollectionWindow(CancellationToken ct)
        {
            var collectionData = await _cardCollectionReader.Load(ct);
            
            var newCardsData = CardCollectionNewCardsDto.Create(collectionData);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                //TODO check do i need here await
                await _cardCollectionModule.ResetNewFlagsAsync(newCardIds, ct);
            }

            _collectionProgressSnapshotService.TryGetSnapshot(out var snapshot);
            var args = new CardCollectionArgs(
                newCardsData,
                collectionData,
                _exchangeOfferProvider,
                _rewardDefinitionFactory, 
                _pointsAccount,
                snapshot);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(collectionData);
        }
    }
}
