using UnityEngine;
using UnityEngine.Rendering;

namespace Fabros.TileEditor
{
    public class SortingLayerHelper : MonoBehaviour
    {
        [SerializeField] private SortingGroup _sortingGroup;

        public void SetSortingOrder(float orderId)
        {
            _sortingGroup.sortingOrder = (int)orderId;
        }
        
        public void SetSortingLayer(int sortingLayerId)
        {
            switch (sortingLayerId)
            {
                case 0:
                    _sortingGroup.sortingLayerName = "EvnBack";
                    break;
                case 1:
                    _sortingGroup.sortingLayerName = "Tile";
                    break;
                case 2:
                    _sortingGroup.sortingLayerName = "EnvFront";
                    break;
                default:
                    Debug.LogWarning($"Failed to set sorting layer with ID {sortingLayerId}");
                    break;
            }
        }
        
        public void SetPlus30() => SetSortingOrderByGroup(SortingOrder.Plus30);
        public void SetPlus20() => SetSortingOrderByGroup(SortingOrder.Plus20);
        public void SetPlus10() => SetSortingOrderByGroup(SortingOrder.Plus10);
        public void SetZero() => SetSortingOrderByGroup(SortingOrder.Zero);
        public void SetMinus10() => SetSortingOrderByGroup(SortingOrder.Minus10);
        public void SetMinus20() => SetSortingOrderByGroup(SortingOrder.Minus20);
        public void SetMinus30() => SetSortingOrderByGroup(SortingOrder.Minus30);
        
        private void SetSortingOrderByGroup(SortingOrder orderId)
        {
            _sortingGroup.sortingOrder = (int)orderId;
        }
        
        private enum SortingOrder
        {
            Plus30 = 30,
            Plus20 = 20,
            Plus10 = 10,
            Zero = 0,
            Minus10 = -10,
            Minus20 = -20,
            Minus30 = -30
        }
    }
}