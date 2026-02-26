using UISystem;
using UnityEngine;

namespace core
{
    public class ContentWidgetArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly OfferContent OfferContent;
        public readonly RectTransform RectTransform;
        
        public ContentWidgetArgs(UIManager uiManager, OfferContent offerContent, RectTransform rectTransform)
        {
            UiManager = uiManager;
            OfferContent = offerContent;
            RectTransform = rectTransform;
        }
    }
    
    [Window("ContentWidget", WindowType.Widget)]
    public class ContentWidgetController : WindowController<ContentWidgetView>
    {
        private ContentWidgetArgs Args => (ContentWidgetArgs)Arguments;
        
        protected override void OnShowStart()
        {
            View.ShowContentView((BaseOfferContent)Args.OfferContent, Args.RectTransform);
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