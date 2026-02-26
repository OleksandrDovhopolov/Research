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
        public readonly Action OnPointsAmountChangedHandler;
        
        public CollectionPointsExchangeArgs(UIManager uiManager,
            int pointsAmount,
            IExchangePackProvider exchangePackProvider, Action onPointsAmountChangedHandler)
        {
            UiManager = uiManager;
            PointsAmount = pointsAmount;
            ExchangePackProvider = exchangePackProvider;
            OnPointsAmountChangedHandler = onPointsAmountChangedHandler;
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
            
            TryHideContentWidget();
            
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
                if (spent)
                {
                    if (Args.ExchangePackProvider.ReceivePackContent(packName))
                    {
                        Args.OnPointsAmountChangedHandler?.Invoke();
                        CloseWindow();
                    }
                    else
                    {
                        ShowInfoWidget("Failed to open pack");
                    }
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

        private void OnInfoPackClickedHandler(string packName, RectTransform rectTransform)
        {
            OnInfoPackClickedHandlerAsync(packName, rectTransform, _buyCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTask OnInfoPackClickedHandlerAsync(string packName, RectTransform rectTransform, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(packName))
            {
                return;
            }

            try
            {
                var packContent = await Args.ExchangePackProvider.GetPackContentAsync(packName, ct);
                var args = new ContentWidgetArgs(Args.UiManager, packContent, rectTransform);
                Args.UiManager.Show<ContentWidgetController>(args);
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            TryHideContentWidget();
            
            View.CloseClick -= CloseWindow;
            View.OnPackBuyClicked -= OnBuyPackClickedHandler;
            View.OnPackInfoClicked -= OnInfoPackClickedHandler;
            
            _buyCts?.Cancel();
            _buyCts?.Dispose();
            _buyCts = null;
        }

        private void TryHideContentWidget()
        {
            if (Args.UiManager.IsWindowShown<ContentWidgetController>())
            {
                Args.UiManager.Hide<ContentWidgetController>();
            }
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