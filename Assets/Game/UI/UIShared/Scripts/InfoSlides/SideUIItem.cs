using System;
using DG.Tweening;
using UnityEngine;

namespace UIShared
{
    [Serializable]
    public class SideUIItem
    {
        public RectTransform Item;
        public Side DockSide;
        public float Offset;
        [HideInInspector] public Vector2 StartPosition;
        
        public Tweener Show(float duration)
        {
            return Item.DOAnchorPos(StartPosition, duration).SetEase(Ease.OutBack);
        }
        
        public Tweener Hide(float duration)
        {
            var destination = StartPosition + InfoSlidesPageAnimation.GetSideOffset(DockSide, Offset);
            return Item.DOAnchorPos(destination, duration).SetEase(Ease.OutBack);
        }
            
        public enum Side
        {
            Top = 0,
            Bottom = 1,
            Left = 2,
            Right = 3
        }
    }
}