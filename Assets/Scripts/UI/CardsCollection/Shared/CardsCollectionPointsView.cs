using TMPro;
using UnityEngine;

namespace core
{
    public class CardsCollectionPointsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private RectTransform _pointsContainer;
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _pointAmount.text = pointsAmount.ToString();
        }
    }
}