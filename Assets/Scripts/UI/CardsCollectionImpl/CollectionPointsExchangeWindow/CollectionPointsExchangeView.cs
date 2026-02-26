using System;
using System.Collections.Generic;
using TMPro;
using UISystem;
using UnityEngine;

namespace core
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

            foreach (var exchangePackEntry in exchangeOfferProvider.GetAllOffers())
            {
                var packView = _exchangePackPool.GetNext();

                packView.OnButtonClicked += OnBuyPackClickedHandler;
                packView.OnPackClicked += OnInfoPackClickedHandler;
                packView.SetData(pointsAmount, exchangePackEntry);
                
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