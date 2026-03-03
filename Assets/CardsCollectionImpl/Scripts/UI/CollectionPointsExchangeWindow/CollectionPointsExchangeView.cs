using System;
using System.Collections.Generic;
using CardCollection.Core;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CollectionPointsExchangeView : WindowView
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        
        [Space, Space, Header("ExchangePackPool")]
        [SerializeField] private UIListPool<ExchangePackView> _exchangePackPool;

        private readonly List<ExchangePackView> _exchangePacks = new();
        
        public event Action<string> OnOfferBuyClicked;
        public event Action<string, RectTransform> OnOfferInfoClicked;
        
        public void CreateView(int pointsAmount, IExchangeOfferProvider exchangeOfferProvider)
        {
            _pointAmount.text = pointsAmount.ToString();
            
            _exchangePacks.Clear();
            _exchangePackPool.DisableNonActive();

            foreach (var exchangeOffer in exchangeOfferProvider.GetAllOffers())
            {
                var packView = _exchangePackPool.GetNext();

                packView.OnButtonClicked += OnBuyPackClickedHandler;
                packView.OnPackClicked += OnInfoPackClickedHandler;
                packView.SetData(pointsAmount, exchangeOffer);
                
                _exchangePacks.Add(packView);
            }
        }

        private void OnBuyPackClickedHandler(string offerPackId)
        {
            OnOfferBuyClicked?.Invoke(offerPackId);
        }

        private void OnInfoPackClickedHandler(string offerPackId, RectTransform rectTransform)
        {
            OnOfferInfoClicked?.Invoke(offerPackId, rectTransform);
        }
        
        public void DisableAll()
        {
            foreach (var packView in _exchangePacks)
            {
                packView.OnButtonClicked -= OnBuyPackClickedHandler;
                packView.OnPackClicked -= OnInfoPackClickedHandler;
            }
            _exchangePacks.Clear();
            
            _exchangePackPool.DisableAll();
        }
    }
}