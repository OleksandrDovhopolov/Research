using UISystem;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        
        public CardGroupArgs(UIManager uiManager)
        {
            UiManager = uiManager;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private UIManager _uiManager;
        
        private CardGroupArgs Args => (CardGroupArgs) Arguments;
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CardGroupController>();
        }
    }
}