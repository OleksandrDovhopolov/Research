using UISystem;
using UnityEngine;

namespace core
{
    public class ContentWidgetArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly PackContent PackContent;
        public readonly RectTransform RectTransform;
        
        public ContentWidgetArgs(UIManager uiManager, PackContent packContent, RectTransform rectTransform)
        {
            UiManager = uiManager;
            PackContent = packContent;
            RectTransform = rectTransform;
        }
    }
    
    [Window("ContentWidget", WindowType.Widget)]
    public class ContentWidgetController : WindowController<ContentWidgetView>
    {
        private ContentWidgetArgs Args => (ContentWidgetArgs)Arguments;
        
        protected override void OnShowStart()
        {
            View.ShowContentView((BasePackContent)Args.PackContent, Args.RectTransform);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<ContentWidgetController>();
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        protected override void OnHideComplete(bool isClosed)
        {
        }
    }
}