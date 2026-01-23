using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;

namespace core
{
    public class NewCardArgs : WindowArgs
    {
        public readonly CardPack CardPack;
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CollectionModule;

        public NewCardArgs(CardPack cardPack, UIManager uiManager, ICardCollectionModule collectionModule)
        {
            CardPack = cardPack;
            UiManager = uiManager;
            CollectionModule = collectionModule;
        }
    }

    [Window("NewCardWindow")]
    public class NewCardController : WindowController<NewCardView>
    {
        private NewCardArgs Args => (NewCardArgs)Arguments;

        protected override void OnShowStart()
        {
            GetNewCardsAsync().Forget();
        }

        private async UniTask GetNewCardsAsync()
        {
            var cardsIdList = await Args.CollectionModule.OpenPackAndUnlockAsync(Args.CardPack);
            var cardsData = await Args.CollectionModule.GetCardsByIdsAsync(cardsIdList);
            var displayData = cardsData.ToNewCardDisplayData();
            
            View.CreateNewCards(displayData);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            base.OnHideComplete(isClosed);

            View.DisableAll();
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<NewCardController>();
        }
    }
}