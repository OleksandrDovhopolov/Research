using TMPro;
using UnityEngine;

namespace core
{
    public class CardsCollectionPointsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private RectTransform _pointsContainer;
        
        private int _currentPoints;
        
        public void SetPointsAmount(int pointsAmount)
        {
            _currentPoints = pointsAmount;
            _pointAmount.text = _currentPoints.ToString();
        }
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _currentPoints += pointsAmount;
            _pointAmount.text = _currentPoints.ToString();
        }
    }
}