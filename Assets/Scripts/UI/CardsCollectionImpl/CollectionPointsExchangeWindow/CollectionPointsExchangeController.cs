using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

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
            View.OnPackBuyClicked += OnBuyPackClickedHandler;
            View.OnPackInfoClicked += OnInfoPackClickedHandler;
        }
        

        private void OnBuyPackClickedHandler(string packName)
        {
            OnBuyPackClickedHandlerAsync(packName).Forget();
        }

        private async UniTask OnBuyPackClickedHandlerAsync(string packName)
        {
            var packPrice = Args.ExchangePackProvider.GetPackPrice(packName);
            var isBuyAvailable = Args.PointsAmount >= packPrice;
            
            Debug.LogWarning($"Debug pack {packName} clicked, packPrice {packPrice}, isBuyAvailable {isBuyAvailable}"); 
            
            if (isBuyAvailable)
            {
                var result = await Args.ExchangePackProvider.TrySpendPointsAsync(packPrice);
                if (!result)
                {
                    const string infoText = "Failed to spend points";
                    ShowInfoWidget(infoText);
                }
            }
            else
            {
                const string infoText = "Not enough stars to open this chest";
                ShowInfoWidget(infoText);
            }
        }
        
        private void ShowInfoWidget(string infoText)
        {
            var infoArgs = new InfoWidgetArg(Args.UiManager, infoText);
            Args.UiManager.Show<InfoWidgetController>(infoArgs);
        }

        private void OnInfoPackClickedHandler(string packName)
        {
            //TODO create general info window  
            // https://www.notion.so/Create-UI-system-for-panel-with-data-30b511859db380158289c4dd393a48c8?v=49ab588c8e164a33aa3b0ecd61d096d0&source=copy_link
            Debug.LogWarning($"Debug pack {packName} clicked");
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnPackBuyClicked -= OnBuyPackClickedHandler;
            View.OnPackInfoClicked -= OnInfoPackClickedHandler;
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