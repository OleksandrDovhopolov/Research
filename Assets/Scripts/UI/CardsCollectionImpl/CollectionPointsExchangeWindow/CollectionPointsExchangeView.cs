using TMPro;
using UISystem;
using UnityEngine;

namespace core
{
    public class CollectionPointsExchangeView : WindowView
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;

        public void SetPointsAmount(int pointsAmount)
        {
            Debug.LogWarning($"Debug CollectionPointsExchangeView pointsAmount {pointsAmount}");
            _pointAmount.text = pointsAmount.ToString();
        }
    }
}