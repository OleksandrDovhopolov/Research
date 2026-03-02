using System;
using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CollectionPointsExchangeArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly int PointsAmount;
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        public readonly Action OnPointsAmountChangedHandler;
        
        public CollectionPointsExchangeArgs(
            UIManager uiManager,
            int pointsAmount,
            IExchangeOfferProvider exchangeOfferProvider, 
            Action onPointsAmountChangedHandler = null)
        {
            UiManager = uiManager;
            PointsAmount = pointsAmount;
            ExchangeOfferProvider = exchangeOfferProvider;
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
            View.CreateView(Args.PointsAmount, Args.ExchangeOfferProvider);
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnOfferBuyClicked += OnBuyOfferClickedHandler;
            View.OnOfferInfoClicked += OnInfoOfferClickedHandler;
        }
        
        private void OnBuyOfferClickedHandler(string offerPackId)
        {
            OnBuyPackClickedHandlerAsync(offerPackId, _buyCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTask OnBuyPackClickedHandlerAsync(string offerPackId, CancellationToken ct)
        {
            if (_isPurchaseInProgress || string.IsNullOrWhiteSpace(offerPackId))
                return;
            
            TryHideContentWidget();
            
            var packPrice = Args.ExchangeOfferProvider.GetOfferPrice(offerPackId);
            if (packPrice <= 0)
            {
                ShowInfoWidget($"Invalid offerPackId {offerPackId} with zero price ");
                return;
            }
            
            _isPurchaseInProgress = true;
            
            try
            {
                var spent = await Args.ExchangeOfferProvider.TrySpendCollectionPointsAsync(packPrice, ct);
                if (spent)
                {
                    if (await Args.ExchangeOfferProvider.ReceiveOfferContent(offerPackId, ct))
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

        private void OnInfoOfferClickedHandler(string packName, RectTransform rectTransform)
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
                OfferContent packContent = await Args.ExchangeOfferProvider.GetOfferContentAsync(packName, ct);
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
            View.OnOfferBuyClicked -= OnBuyOfferClickedHandler;
            View.OnOfferInfoClicked -= OnInfoOfferClickedHandler;
            
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