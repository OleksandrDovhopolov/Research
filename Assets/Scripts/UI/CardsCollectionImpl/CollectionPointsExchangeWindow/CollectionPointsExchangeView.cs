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
        
        public void CreateView(int pointsAmount, IExchangePackProvider exchangePackProvider)
        {
            _pointAmount.text = pointsAmount.ToString();
            
            _exchangePacks.Clear();
            _exchangePackPool.DisableNonActive();

            foreach (var exchangePackEntry in exchangePackProvider.GetAllPacks())
            {
                var packView = _exchangePackPool.GetNext();

                packView.OnButtonClicked += OnPackClickedHandler;
                packView.SetData(pointsAmount, exchangePackEntry);
                
                _exchangePacks.Add(packView);
            }
        }

        private void OnPackClickedHandler(string packName)
        {
            Debug.LogWarning($"Debug pack {packName} clicked");
        }
        
        public void DisableAll()
        {
            foreach (var packView in _exchangePacks)
            {
                packView.OnButtonClicked -= OnPackClickedHandler;
            }
            _exchangePacks.Clear();
            
            _exchangePackPool.DisableAll();
        }
    }
}