using CardCollectionImpl;
using UISystem;
using UnityEngine;

namespace core
{
    public class ContentWidgetArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly CardCollectionImpl.CollectionRewardDefinition CollectionRewardDefinition;
        public readonly RectTransform RectTransform;
        
        public ContentWidgetArgs(UIManager uiManager, CardCollectionImpl.CollectionRewardDefinition collectionRewardDefinition, RectTransform rectTransform)
        {
            UiManager = uiManager;
            CollectionRewardDefinition = collectionRewardDefinition;
            RectTransform = rectTransform;
        }
    }
    
    //TODO try remove this dependency using CardCollectionImpl;. ContentWidgetController is UIShared. should it know about CardCollectionImpl ? no. 
    // CardCollectionImpl knows about UIShared and game (core) knows about UIShared
    [Window("ContentWidget", WindowType.Widget)]
    public class ContentWidgetController : WindowController<ContentWidgetView>
    {
        private ContentWidgetArgs Args => (ContentWidgetArgs)Arguments;
        
        protected override void OnShowStart()
        {
            View.ShowContentView(Args.CollectionRewardDefinition, Args.RectTransform);
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