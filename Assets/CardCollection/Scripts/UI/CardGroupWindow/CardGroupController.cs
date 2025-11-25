using System.Collections.Generic;
using UISystem;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly IReadOnlyList<CardModel> CardsData;
        
        public CardGroupArgs(UIManager uiManager, IReadOnlyList<CardModel> cardsData)
        {
            UiManager = uiManager;
            CardsData = cardsData;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private UIManager _uiManager;
        
        private CardGroupArgs Args => (CardGroupArgs) Arguments;

        protected override void OnShowStart()
        {
            View.Configure(Args.CardsData);
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
            View.DestroyCards();
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<CardGroupController>();
        }
    }
}