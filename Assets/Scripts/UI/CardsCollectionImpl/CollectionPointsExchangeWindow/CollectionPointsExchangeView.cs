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
        
        public event Action<string> OnPackBuyClicked;
        public event Action<string, RectTransform> OnPackInfoClicked;
        
        public void CreateView(int pointsAmount, IExchangePackProvider exchangePackProvider)
        {
            _pointAmount.text = pointsAmount.ToString();
            
            _exchangePacks.Clear();
            _exchangePackPool.DisableNonActive();

            foreach (var exchangePackEntry in exchangePackProvider.GetAllPacks())
            {
                var packView = _exchangePackPool.GetNext();

                packView.OnButtonClicked += OnBuyPackClickedHandler;
                packView.OnPackClicked += OnInfoPackClickedHandler;
                packView.SetData(pointsAmount, exchangePackEntry);
                
                _exchangePacks.Add(packView);
            }
        }

        private void OnBuyPackClickedHandler(string packName)
        {
            OnPackBuyClicked?.Invoke(packName);
        }

        private void OnInfoPackClickedHandler(string packName, RectTransform rectTransform)
        {
            OnPackInfoClicked?.Invoke(packName, rectTransform);
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