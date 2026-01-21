using Cysharp.Threading.Tasks;
using UISystem;

namespace core
{
     public class NewCardArgs : WindowArgs
        {
            public readonly UIManager UiManager;
            public readonly PackBasedCardsRandomizer CardRandomizer;
            public readonly ICollectionUpdater CollectionUpdater;
            
            public NewCardArgs(UIManager uiManager, PackBasedCardsRandomizer cardRandomizer, ICollectionUpdater collectionUpdater)
            {
                UiManager = uiManager;
                CardRandomizer = cardRandomizer;
                CollectionUpdater = collectionUpdater;
            }
        }
        
    [Window("NewCardWindow")]
    public class NewCardController :  WindowController<NewCardView>
    {
        private NewCardArgs Args => (NewCardArgs) Arguments;
        
        protected override void OnShowStart()
        {
            GetNewCardsAsync().Forget();
        }
        
        private async UniTask GetNewCardsAsync()
        { 
            var newCardsId = await Args.CardRandomizer.GetRandomNewCardsAsync();
            await Args.CollectionUpdater.UnlockCard(newCardsId);
            
            var newCardsData = await Args.CollectionUpdater.GetCardsByIds(newCardsId);
            
            var displayData = newCardsData.ToNewCardDisplayData();
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