using TMPro;
using UnityEngine;

namespace core
{
    public class CollectionPointView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private RectTransform _container;
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _pointAmount.text = pointsAmount.ToString();
        }
    }
}