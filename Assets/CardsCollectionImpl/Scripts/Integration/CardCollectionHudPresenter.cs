using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionHudPresenter 
    {
        private const string CardCollectionButtonId = "CardCollection";
        
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly ICardCollectionReader _reader;
        private readonly ICardCollectionModule _module;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;
        
        private IEventButton _eventButton;
        
        public CardCollectionHudPresenter(
            UIManager uiManager,
            IHUDService hudService,
            ICardCollectionReader reader,
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICollectionProgressSnapshotService collectionProgressSnapshotService)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardDefinitionFactory = rewardDefinitionFactory ?? throw new ArgumentNullException(nameof(rewardDefinitionFactory));
            _collectionProgressSnapshotService = collectionProgressSnapshotService ?? throw new ArgumentNullException(nameof(collectionProgressSnapshotService));
        }

        public void Bind(ScheduleItem config, CancellationToken ct)
        {
            var entryButton = _hudService.SpawnEventButton(CardCollectionButtonId);

            if (entryButton == null)
            {
                Debug.LogWarning($"[CardCollectionRuntime] No button found for {CardCollectionButtonId}]");
                return;
            }
            
            _eventButton = entryButton;
            _eventButton.Setup(config, () => OnEventButtonClickHandler(ct), ct);
            _eventButton.SetVisible(true);
        }

        private void OnEventButtonClickHandler(CancellationToken ct)
        {
            OpenCardCollectionWindow(ct).Forget();
        }
        
        public async UniTask OpenCardCollectionWindow(CancellationToken ct)
        {
            var collectionData = await _reader.Load(ct);
            
            var newCardsData = CardCollectionNewCardsDto.Create(collectionData);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                //TODO check do i need here await
                await _module.ResetNewFlagsAsync(newCardIds, ct);
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
        
        public void Unbind()
        {
            _eventButton = null;
            _hudService.RemoveEventButton(CardCollectionButtonId);
        }
        
        public void Dispose()
        {
            
        }
    }
}