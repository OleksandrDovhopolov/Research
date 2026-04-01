using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Rewards;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CollectionPointsExchangeArgs : WindowArgs
    {
        public readonly int PointsAmount;
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        public readonly ICardCollectionPointsAccount CardCollectionPointsAccount;
        public readonly Action OnPointsAmountChangedHandler;
        public readonly IRewardSpecProvider RewardSpecProvider;
        
        public CollectionPointsExchangeArgs(
            int pointsAmount,
            IExchangeOfferProvider exchangeOfferProvider, 
            IRewardSpecProvider rewardSpecProvider,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            Action onPointsAmountChangedHandler = null)
        {
            PointsAmount = pointsAmount;
            ExchangeOfferProvider = exchangeOfferProvider;
            RewardSpecProvider = rewardSpecProvider;
            CardCollectionPointsAccount = cardCollectionPointsAccount;
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
            
            var packPrice = Args.ExchangeOfferProvider.GetOfferPrice(offerPackId);
            if (packPrice <= 0)
            {
                ShowInfoWidget($"Invalid offerPackId {offerPackId} with zero price ");
                return;
            }
            
            _isPurchaseInProgress = true;
            
            try
            {
                var spent = await Args.CardCollectionPointsAccount.TrySpendPointsAsync(packPrice, ct);
                if (spent)
                {
                    if (await Args.ExchangeOfferProvider.ReceiveOfferContent(offerPackId, ct))
                    {
                        ShowInfoWidget("Pack received successfully");
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
            var infoArgs = new InfoWidgetArg(infoText);
            UIManager.Show<InfoWidgetController>(infoArgs);
        }

        private void OnInfoOfferClickedHandler(string packName, RectTransform rectTransform)
        {
            if (string.IsNullOrWhiteSpace(packName)) return;

            if (Args.RewardSpecProvider.TryGet(packName, out var spec))
            {
                var contentWidgetData = spec.ToContentWidgetData();
                var args = new ContentWidgetArgs(contentWidgetData, rectTransform);
                UIManager.Show<ContentWidgetController>(args);
            }
            else
            {
                Debug.LogWarning($"{GetType().Name} failed to find reward with Id {packName}");
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
            if (UIManager.IsWindowShown<ContentWidgetController>())
            {
                UIManager.Hide<ContentWidgetController>();
            }
        }
        
        protected override void OnHideComplete(bool isClosed)
        {
            View.DisableAll();
        }

        private void CloseWindow()
        {
            UIManager.Hide<CollectionPointsExchangeController>();
        }
    }
}