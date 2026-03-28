using UISystem;
using UnityEngine;

namespace UIShared
{
    public class ContentWidgetArgs : WindowArgs
    {
        public readonly ContentWidgetDataBase ContentWidgetData;
        public readonly RectTransform RectTransform;
        
        public ContentWidgetArgs(ContentWidgetDataBase contentWidgetData, RectTransform rectTransform)
        {
            ContentWidgetData = contentWidgetData;
            RectTransform = rectTransform;
        }
    }

    [Window("ContentWidget", WindowType.Widget)]
    public class ContentWidgetController : WindowController<ContentWidgetView>
    {
        private ContentWidgetArgs Args => (ContentWidgetArgs)Arguments;
        
        protected override void OnShowStart()
        {
            View.ShowContentView(Args.ContentWidgetData, Args.RectTransform);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<ContentWidgetController>();
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