using UISystem;
using UnityEngine;

namespace UIShared
{
    public class ContentWidgetArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly ContentWidgetDataBase ContentWidgetData;
        public readonly RectTransform RectTransform;
        
        public ContentWidgetArgs(UIManager uiManager, ContentWidgetDataBase contentWidgetData, RectTransform rectTransform)
        {
            UiManager = uiManager;
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
            View.InventoryButtonClicked += OnInventoryButtonClickedHandler;
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<ContentWidgetController>();
        }

        private void OnInventoryButtonClickedHandler()
        {
            if (Args.ContentWidgetData is InventoryWidgetData inventoryWidgetData)
            {
                inventoryWidgetData.ButtonPressed?.Invoke(inventoryWidgetData.ItemId);
            }
            else
            {
                Debug.LogWarning($"OnInventoryButtonClickedHandler failed. ContentWidgetData is not InventoryWidgetData");
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.InventoryButtonClicked -= OnInventoryButtonClickedHandler;
        }

        protected override void OnHideComplete(bool isClosed)
        {
        }
    }
}