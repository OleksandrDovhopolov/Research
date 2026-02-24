using UISystem;

namespace core
{
    public class CollectionPointsExchangeArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly int PointsAmount;
        public readonly IExchangePackProvider ExchangePackProvider;
        
        public CollectionPointsExchangeArgs(UIManager uiManager,  int pointsAmount,  IExchangePackProvider exchangePackProvider)
        {
            UiManager = uiManager;
            PointsAmount = pointsAmount;
            ExchangePackProvider = exchangePackProvider;
        }
    }
    
    [Window("CollectionPointsExchangeWindow", WindowType.Popup)]
    public class CollectionPointsExchangeController : WindowController<CollectionPointsExchangeView>
    {
        private CollectionPointsExchangeArgs Args => (CollectionPointsExchangeArgs) Arguments;
        
        protected override void OnShowStart()
        {
            View.CreateView(Args.PointsAmount, Args.ExchangePackProvider);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            View.DisableAll();
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CollectionPointsExchangeController>();
        }
    }
}