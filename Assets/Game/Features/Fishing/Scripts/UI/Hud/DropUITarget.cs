using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Fishing
{
    public class DropUITarget : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;

        public bool IsPositionInsideRect(Vector3 position)
        {
            var canvasPosition = _rectTransform.InverseTransformPoint(position);
            return _rectTransform.rect.Contains(canvasPosition);
        }
    }
}