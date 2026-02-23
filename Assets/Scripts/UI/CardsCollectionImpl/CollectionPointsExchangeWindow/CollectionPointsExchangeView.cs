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

        public void SetPointsAmount(int pointsAmount)
        {
            Debug.LogWarning($"Debug CollectionPointsExchangeView pointsAmount {pointsAmount}");
            _pointAmount.text = pointsAmount.ToString();
        }
    }
}