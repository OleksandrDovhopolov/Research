using System;
using System.Threading;
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
        
        private CancellationTokenSource _buyCts;
        private bool _isPurchaseInProgress;
        
        protected override void OnShowStart()
        {
            _buyCts = new CancellationTokenSource();
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
            OnBuyPackClickedHandlerAsync(packName, _buyCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTask OnBuyPackClickedHandlerAsync(string packName, CancellationToken ct)
        {
            if (_isPurchaseInProgress || string.IsNullOrWhiteSpace(packName))
                return;
            
            var packPrice = Args.ExchangePackProvider.GetPackPrice(packName);
            if (packPrice <= 0)
            {
                ShowInfoWidget($"Invalid pack {packName} with zero price ");
                return;
            }
            
            _isPurchaseInProgress = true;
            
            try
            {
                var spent = await Args.ExchangePackProvider.TrySpendPointsAsync(packPrice, ct);
                
                Debug.LogWarning($"Debug pack {packName} clicked, packPrice {packPrice}"); 
                if (spent)
                {
                    if (!Args.ExchangePackProvider.ReceivePackContent(packName))
                        ShowInfoWidget("Failed to open pack");
                }
                else
                {
                    ShowInfoWidget("Not enough stars to open this chest");
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isPurchaseInProgress = false;
            }
        }
        
        private void ShowInfoWidget(string infoText)
        {
            var infoArgs = new InfoWidgetArg(Args.UiManager, infoText);
            Args.UiManager.Show<InfoWidgetController>(infoArgs);
        }

        private void OnInfoPackClickedHandler(string packName)
        {
            var packContent = Args.ExchangePackProvider.GetPackContent(packName);
            //TODO create general info window  
            // https://www.notion.so/Create-UI-system-for-panel-with-data-30b511859db380158289c4dd393a48c8?v=49ab588c8e164a33aa3b0ecd61d096d0&source=copy_link
            Debug.LogWarning($"Debug pack {packName} clicked. Content  {packContent.GemsAmount}");
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnPackBuyClicked -= OnBuyPackClickedHandler;
            View.OnPackInfoClicked -= OnInfoPackClickedHandler;
            
            _buyCts?.Cancel();
            _buyCts?.Dispose();
            _buyCts = null;
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