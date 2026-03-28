using UISystem;

namespace UIShared
{
    public class InfoWidgetArg : WindowArgs
    {
        public readonly string Text;
        
        public InfoWidgetArg(string text)
        {
            Text = text;
        }
    }
    
    [Window("info_widget", WindowType.Widget)]
    public class InfoWidgetController : WindowController<InfoWidgetView>
    {
        private InfoWidgetArg Args => (InfoWidgetArg) Arguments;
        
        protected override void OnShowComplete()
        {
            View.OnComplete += CloseWidget;
        }
        
        public override void UpdateWindow()
        {
            View.ShowHint(Args.Text);
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.OnComplete -= CloseWidget;
        }

        private void CloseWidget()
        {
            UIManager.Hide<InfoWidgetController>();
        }
    }
}