using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class InfoSlidesPageAnimation : WindowAnimation
    {
        [SerializeField] private Image _darkBG;
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private float _fadeInDelay = 0.2f;
        [SerializeField] private float _showDuration = 0.4f;
        [SerializeField] private CanvasGroup _tapButtonCanvasGroup;
        [SerializeField] private float _tapButtonDelay = .35f;
        [SerializeField] private List<InfoSlideUIItem> _items;
        
        private float _mainOffset = Screen.height;
        private float _targetAlpha;
        
        public override float ShowAnimationTime => _showDuration;
        
        private void Awake()
        {
            _mainOffset = Screen.height / 3f;
            _targetAlpha = _darkBG.color.a;
            _items.ForEach(i => i.StartPosition = i.Item.anchoredPosition);
        }

        public override IEnumerator AnimationIn()
        {
            SetDefaultsValues();
            
            var sequence = DOTween.Sequence()
                .Join(_darkBG.DOFade(_targetAlpha, ShowAnimationTime))
                .Insert(_fadeInDelay, _rootCanvasGroup.DOFade(1, ShowAnimationTime - _fadeInDelay).From(0))
                .Insert(_tapButtonDelay, _tapButtonCanvasGroup.DOFade(1, ShowAnimationTime - _tapButtonDelay));
            
            foreach (var item in _items)
            {
                sequence.Insert(item.AppearDelay, item.Item.DOAnchorPos(item.StartPosition, item.Duration).SetEase(item.AppearCurve));
            }
            
            yield return sequence.WaitForCompletion();
        }

        public override IEnumerator AnimationOut(float animationTime)
        {
            var sequence = DOTween.Sequence()
                .Join(((RectTransform)_rootCanvasGroup.transform).DOAnchorPos(new Vector2(0, _mainOffset), animationTime))
                .Join(_darkBG.DOFade(0, animationTime - _fadeInDelay))
                .Join(_tapButtonCanvasGroup.DOFade(0, animationTime).From(1));
            sequence.Join(_rootCanvasGroup.DOFade(0, animationTime).From(1));
            
            yield return sequence.SetEase(Ease.Linear).WaitForCompletion();
        }

        private void SetDefaultsValues()
        {
            foreach (var item in _items)
            {
                item.Item.anchoredPosition = item.StartPosition + GetSideOffset(item.DockSide, _mainOffset);
            }

            _tapButtonCanvasGroup.alpha = 0;
            _darkBG.color = new Color(_darkBG.color.r, _darkBG.color.g, _darkBG.color.b, 0);
            ((RectTransform)_rootCanvasGroup.transform).anchoredPosition = Vector2.zero;
        }
        
        
        public static Vector2 GetSideOffset(SideUIItem.Side side, float mainOffset)
        {
            return side switch
            {
                SideUIItem.Side.Top => Vector2.up * mainOffset,
                SideUIItem.Side.Bottom => Vector2.down * mainOffset,
                SideUIItem.Side.Left => Vector2.left * mainOffset,
                SideUIItem.Side.Right => Vector2.right * mainOffset,
                _ => Vector2.zero
            };
        }
    }
}