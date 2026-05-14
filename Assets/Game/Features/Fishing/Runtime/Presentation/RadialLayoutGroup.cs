using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Fishing
{
    public class RadialLayoutGroup : MonoBehaviour
    {
        [Header("Options")] 
        [SerializeField] private float _radius;
        [SerializeField] private float _spaceAngle;
        [Range(1, ushort.MaxValue)]
        [SerializeField] private ushort _maxCountInRow;
        [SerializeField] private float _offsetBetweenRows;
        [SerializeField] private float _startAngle;
        [SerializeField] private bool _needAttachPivotToFirstElement;
        [SerializeField] private bool _updateItemsRotation;
        [SerializeField] private bool _inverseOrder;
        [SerializeField] private Vector3 _itemsOffset;
        
        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }
        
        public float SpaceAngle
        {
            get => _spaceAngle;
            set => _spaceAngle = value;
        }
        
        public ushort MaxCountInRow
        {
            get => _maxCountInRow;
            set => _maxCountInRow = value;
        }

        public Vector3 ItemsOffset
        {
            get { return _itemsOffset; }
            set { _itemsOffset = value; }
        }


        #if UNITY_EDITOR

        private void OnValidate()
        {
            MaxCountInRow = _maxCountInRow;
            UpdateRadialLayoutGroup();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f,1f,0f, 0.9f);
            var childElements = GetActiveElements();
            var rowsCount = childElements.Count / _maxCountInRow;
            var direction = Quaternion.AngleAxis(_startAngle, -Vector3.forward) * new Vector3(0, _radius, 0);
            Gizmos.DrawWireSphere(transform.position - direction + _itemsOffset, _radius);
            for (var i = 0; i < childElements.Count; i++)
            {
                var currentRow = i / _maxCountInRow;
                var indexInRow = i % _maxCountInRow;
                var elementsInRow = currentRow == rowsCount ? childElements.Count % _maxCountInRow : _maxCountInRow;
                var rowAngleOffset = (elementsInRow - 1) / 2f * _spaceAngle;
                var angle = -rowAngleOffset + _spaceAngle * indexInRow;
                var position = Quaternion.AngleAxis(angle, -Vector3.forward) * direction;

                position -= (_needAttachPivotToFirstElement ? direction : Vector3.zero) + (_offsetBetweenRows * currentRow) * direction.normalized;
                Gizmos.DrawWireSphere(transform.position + position + _itemsOffset, 100f);
                
                if (i % _maxCountInRow != 0)
                {
                    var previousPosition = Quaternion.AngleAxis(angle - _spaceAngle, -Vector3.forward) * direction;
                    previousPosition -= (_needAttachPivotToFirstElement ? direction : Vector3.zero) + (_offsetBetweenRows * currentRow) * direction.normalized;
                    Gizmos.DrawLine(transform.position + previousPosition + _itemsOffset, transform.position + position + _itemsOffset);
                }
            }
                        
            for (var i = -1; i < 2; i++)
            {
                var sectorAngle = 360 - _startAngle + i * _spaceAngle;
                var sectorDirection = Quaternion.AngleAxis(sectorAngle, Vector3.forward) * new Vector3(0, _radius, 0);
                Gizmos.DrawLine(transform.position - direction + _itemsOffset, transform.position + sectorDirection - direction + _itemsOffset);
            }
        }

#endif
        
        private async void OnTransformChildrenChanged()
        {
            await UniTask.DelayFrame(1);
            UpdateRadialLayoutGroup();
        }

        private void Awake()
        {
            MaxCountInRow = _maxCountInRow;
        }

        public void Rebuild() => UpdateRadialLayoutGroup();
        
        private void UpdateRadialLayoutGroup()
        {
            var childElements = GetActiveElements();
            var rowsCount = childElements.Count / _maxCountInRow;
            var direction = Quaternion.AngleAxis(_startAngle, -Vector3.forward) * new Vector3(0, _radius, 0);
            
            for (var i = 0; i < childElements.Count; i++)
            {
                var currentRow = i / _maxCountInRow;
                var indexInRow = i % _maxCountInRow;
                var elementsInRow = currentRow == rowsCount ? childElements.Count % _maxCountInRow : _maxCountInRow;
                var rowAngleOffset = (elementsInRow - 1) / 2f * _spaceAngle;
                var angle = -rowAngleOffset + _spaceAngle * indexInRow;
                var position= Quaternion.AngleAxis(angle, -Vector3.forward) * direction;

                position -= (_needAttachPivotToFirstElement ? direction : Vector3.zero) + (_offsetBetweenRows * currentRow) * direction.normalized;
                var currentElement = _inverseOrder? childElements[^(i + 1)]: childElements[i];
                currentElement.localPosition = position + _itemsOffset;
                if (_updateItemsRotation)
                {
                    currentElement.localRotation =  Quaternion.Euler(new Vector3(0, 0, -angle));
                }
            }
        }

        private List<Transform> GetActiveElements()
        {
            var activeElements = new List<Transform>();
            
            for(int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                
                if(!child.gameObject.activeSelf) continue;
                activeElements.Add(child.transform);
                    
            }

            return activeElements;
        }
    }
}
