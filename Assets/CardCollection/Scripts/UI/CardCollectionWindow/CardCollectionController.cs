using System.Collections.Generic;
using System.Linq;
using UISystem;

namespace core
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        /*
         * CardModel is from config 
         */
        public readonly IReadOnlyList<CardData> CardsModel;
        
        public CardCollectionArgs(UIManager uiManager, IReadOnlyList<CardData> cardsModel)
        {
            UiManager = uiManager;
            CardsModel = cardsModel;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController :  WindowController<CardCollectionView>
    {
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;

        protected override void OnShowStart()
        {
            
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnButtonPressed += OpenGroupWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnButtonPressed -= OpenGroupWindow;
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
        
        private void OpenGroupWindow()
        {
            var sprite = View.Sprite;

            var cardModels = new List<CardModel>();
            
            var grouped = Args.CardsModel.GroupBy(model => model.GroupType);
            foreach (var cardData in grouped.First())
            {
                var cardModel = new CardModel(sprite, cardData.CardName, true, cardData.Stars);
                cardModels.Add(cardModel);
            }
            
            // from CardData(from config) to CardModel (model for view single cards)
            var args = new CardGroupArgs(Args.UiManager, cardModels);
            Args.UiManager.Show<CardGroupController>(args);
        }
    }
}