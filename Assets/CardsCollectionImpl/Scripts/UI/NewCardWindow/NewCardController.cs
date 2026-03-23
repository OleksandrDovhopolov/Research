using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class NewCardArgs : WindowArgs
    {
        public readonly CardPack CardPack;
        public readonly ICardCollectionModule CollectionModule;
        public readonly ICardCollectionReader CollectionReader;
        public readonly ICardCollectionCacheService CardCollectionCacheService;

        public NewCardArgs(
            CardPack cardPack,
            ICardCollectionModule collectionModule,
            ICardCollectionReader collectionReader,
            ICardCollectionCacheService cardCollectionCacheService)
        {
            CardPack = cardPack;
            CollectionModule = collectionModule;
            CollectionReader = collectionReader;
            CollectionReader = collectionReader;
            CardCollectionCacheService = cardCollectionCacheService;
        }
    }

    [Window("NewCardWindow")]
    public class NewCardController : WindowController<NewCardView>
    {
        private ICardsConfigProvider _cardsConfigProvider;
        
        private NewCardArgs Args => (NewCardArgs)Arguments;
        private CancellationTokenSource _cts;

        [Inject]
        private void Construct(ICardsConfigProvider cardsConfigProvider)
        {
            _cardsConfigProvider = cardsConfigProvider;
        }
        
        protected override void OnShowStart()
        {
            _cts = new CancellationTokenSource();
            GetNewCardsAsync(_cts.Token).Forget();
        }

        private async UniTask GetNewCardsAsync(CancellationToken ct)
        {
            var collectionPoints = await Args.CollectionReader.GetCollectionPoints();
            View.UpdatePointsAmount(collectionPoints);
            
            var cardsIdList = await Args.CollectionModule.OpenPackAndUnlockAsync(Args.CardPack, ct);
            var cardsData = await Args.CollectionModule.GetCardsByIdsAsync(cardsIdList, ct);
            var displayData = Args.CardCollectionCacheService.ToNewCardDisplayData(cardsData);
            
            View.CreateNewCards(displayData);
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
