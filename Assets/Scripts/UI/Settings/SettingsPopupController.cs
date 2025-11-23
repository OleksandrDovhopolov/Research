using UISystem;

namespace core
{
    public class SettingsArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        
        public SettingsArgs(UIManager uiManager)
        {
            UiManager = uiManager;
        }
    }
    
    [Window("SettingsWindow")]
    public class SettingsPopupController : WindowController<SettingsPopupView>
    {
        private UIManager _uiManager;
        
        private SettingsArgs Args => (SettingsArgs) Arguments;
        
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
            Args.UiManager.Hide<SettingsPopupController>();
        }
    }
}

