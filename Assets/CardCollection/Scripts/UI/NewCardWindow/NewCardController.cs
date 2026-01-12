using UISystem;

namespace core
{
     public class NewCardArgs : WindowArgs
        {
            public readonly UIManager UiManager;
            
            public NewCardArgs(UIManager uiManager)
            {
                UiManager = uiManager;
            }
        }
        
    [Window("NewCardWindow")]
    public class NewCardController :  WindowController<NewCardView>
    {
        private NewCardArgs Args => (NewCardArgs) Arguments;
        
        protected override void OnShowStart()
        {
        }
        
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
            Args.UiManager.Hide<NewCardController>();
        }
    }
}