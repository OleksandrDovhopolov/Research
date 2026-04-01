using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class NewCardArgs : WindowArgs
    {
        public readonly string EventId;
        public readonly string PackId;
        public readonly ICardCollectionModule CollectionModule;
        public readonly ICardCollectionReader CollectionReader;

        public NewCardArgs(
            string eventId,
            string packId,
            ICardCollectionModule collectionModule,
            ICardCollectionReader collectionReader)
        {
            EventId =  eventId;
            PackId = packId;
            CollectionModule = collectionModule;
            CollectionReader = collectionReader;
            CollectionReader = collectionReader;
        }
    }

    [Window("NewCardWindow")]
    public class NewCardController : WindowController<NewCardView>
    {
        private IEventSpriteManager _eventSpriteManager;
        private ICardCollectionCacheService _cardCollectionCacheService;
        
        private NewCardArgs Args => (NewCardArgs)Arguments;
        private CancellationTokenSource _cts;

        [Inject]
        private void Construct(IEventSpriteManager eventSpriteManager, ICardCollectionCacheService cardCollectionCacheService)
        {
            _eventSpriteManager = eventSpriteManager;
            _cardCollectionCacheService = cardCollectionCacheService;
        }
        
        protected override void OnShowStart()
        {
            _cts = new CancellationTokenSource();
            View.SetSpriteManager(_eventSpriteManager);
            GetNewCardsAsync(_cts.Token).Forget();
        }

        private async UniTask GetNewCardsAsync(CancellationToken ct)
        {
            var collectionPoints = await Args.CollectionReader.GetCollectionPoints();
            View.UpdatePointsAmount(collectionPoints);
            
            var cardsIdList = await Args.CollectionModule.OpenPackAndUnlockAsync(Args.PackId, ct);
            var cardsData = await Args.CollectionModule.GetCardsByIdsAsync(cardsIdList, ct);
            var displayData = _cardCollectionCacheService.ToNewCardDisplayData(cardsData);
            
            View.CreateNewCards(Args.EventId, displayData);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            base.OnHideComplete(isClosed);

            View.DisableAll();
        }

        private void CloseWindow()
        {
            CloseWindowAsync(_cts.Token).Forget();
        }

        private async UniTask CloseWindowAsync(CancellationToken ct)
        {
            View.CloseClick -= CloseWindow;
            await View.PlayCloseSequenceAsync(ct);
            UIManager.Hide<NewCardController>();
        }
    }
}
