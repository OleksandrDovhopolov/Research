using UISystem;

namespace core
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        
        public CardCollectionArgs(UIManager uiManager)
        {
            UiManager = uiManager;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController :  WindowController<SettingsPopupView>
    {
        private UIManager _uiManager;
        
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        
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
            Args.UiManager.Hide<CardCollectionController>();
        }
    }
}