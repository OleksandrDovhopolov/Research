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
    public class CardCollectionController :  WindowController<CardCollectionView>
    {
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        
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
            var args = new CardGroupArgs(Args.UiManager);
            Args.UiManager.Show<CardGroupController>(args, UIShowCommand.UIShowType.Ordered);
        }
    }
}